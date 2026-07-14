#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
RUN_ID="${GITHUB_RUN_ID:-local}-${GITHUB_RUN_ATTEMPT:-0}-$$-${RANDOM}"
RUN_ID="${RUN_ID//[^a-zA-Z0-9_.-]/-}"
IMAGE_TAG="${IMAGE_TAG:-nupack-container-smoke:${RUN_ID}}"
CONTAINER_NAME="nupack-smoke-${RUN_ID}"
VOLUME_NAME="nupack-smoke-${RUN_ID}"
PUBLISH_API_KEY="${NUPACK_SMOKE_PUBLISH_KEY:-publish-${RUN_ID}}"
DELETE_API_KEY="${NUPACK_SMOKE_DELETE_KEY:-delete-${RUN_ID}}"
HEALTH_ATTEMPTS="${HEALTH_ATTEMPTS:-60}"
TEMP_DIR="$(mktemp -d "${TMPDIR:-/tmp}/nupack-container-smoke.XXXXXX")"
CONTAINER_STARTED=0

cleanup() {
    local status=$?
    trap - EXIT

    if (( status != 0 )) && (( CONTAINER_STARTED == 1 )); then
        echo "Container smoke failed; logs from ${CONTAINER_NAME}:" >&2
        docker logs "${CONTAINER_NAME}" >&2 || true
    fi

    docker rm --force "${CONTAINER_NAME}" >/dev/null 2>&1 || true
    docker volume rm --force "${VOLUME_NAME}" >/dev/null 2>&1 || true
    rm -rf "${TEMP_DIR}"
    exit "${status}"
}
trap cleanup EXIT

wait_for_endpoint() {
    local name="$1"
    local url="$2"
    local attempt

    for ((attempt = 1; attempt <= HEALTH_ATTEMPTS; attempt++)); do
        if curl --fail --silent --show-error "${url}" >/dev/null 2>&1; then
            echo "${name} is ready (${url})."
            return 0
        fi
        sleep 2
    done

    echo "Timed out waiting for ${name} after ${HEALTH_ATTEMPTS} attempts: ${url}" >&2
    return 1
}

published_port() {
    local container_port="$1"
    local binding

    binding="$(docker port "${CONTAINER_NAME}" "${container_port}/tcp" | head -n 1)"
    if [[ -z "${binding}" || "${binding##*:}" == "${binding}" ]]; then
        echo "Could not determine published port for ${container_port}/tcp: ${binding}" >&2
        return 1
    fi
    printf '%s\n' "${binding##*:}"
}

restore_consumer() {
    local packages_dir="$1"
    local http_cache_dir="$2"
    local restore_log="$3"

    NUGET_PACKAGES="${packages_dir}" \
    NUGET_HTTP_CACHE_PATH="${http_cache_dir}" \
        dotnet restore "${ROOT_DIR}/tests/Fixtures/Consumer/Consumer.csproj" \
        --configfile "${TEMP_DIR}/nuget.config" \
        --no-cache \
        --force \
        --verbosity detailed 2>&1 | tee "${restore_log}"

    if ! grep -F "Installed TestPackage 1.0.0 from ${API_URL}/v3/index.json" "${restore_log}" >/dev/null; then
        echo "Restore did not prove that TestPackage 1.0.0 came from Nupack (${API_URL})." >&2
        echo "Relevant restore log lines:" >&2
        grep -iE "TestPackage|package source mapping|Nupack" "${restore_log}" >&2 || true
        return 1
    fi
}

command -v docker >/dev/null || { echo "docker is required for the container smoke test." >&2; exit 1; }
command -v dotnet >/dev/null || { echo "dotnet is required for the container smoke test." >&2; exit 1; }
command -v curl >/dev/null || { echo "curl is required for the container smoke test." >&2; exit 1; }

if [[ "${SKIP_BUILD:-0}" == "1" ]]; then
    if ! docker image inspect "${IMAGE_TAG}" >/dev/null 2>&1; then
        echo "SKIP_BUILD=1 but image does not exist locally: ${IMAGE_TAG}" >&2
        exit 1
    fi
    echo "Using existing image ${IMAGE_TAG}."
else
    echo "Building ${IMAGE_TAG} from the repository Dockerfile."
    docker build --tag "${IMAGE_TAG}" "${ROOT_DIR}"
fi

docker volume create "${VOLUME_NAME}" >/dev/null
docker run --detach \
    --name "${CONTAINER_NAME}" \
    --publish 127.0.0.1::5003 \
    --publish 127.0.0.1::5004 \
    --volume "${VOLUME_NAME}:/app/data" \
    --env ASPNETCORE_ENVIRONMENT=Production \
    --env "PackageSecurity__PublishApiKey=${PUBLISH_API_KEY}" \
    --env "PackageSecurity__DeleteApiKey=${DELETE_API_KEY}" \
    --env PackageSecurity__AllowAnonymousWrites=false \
    --env PackageStorage__Provider=FileSystem \
    --env PackageStorage__FileSystem__BasePath=/app/data/packages \
    --env NuGetServer__BaseUrl=http://localhost:5003 \
    "${IMAGE_TAG}" >/dev/null
CONTAINER_STARTED=1

API_PORT="$(published_port 5003)"
WEB_PORT="$(published_port 5004)"
API_URL="http://127.0.0.1:${API_PORT}"
WEB_URL="http://127.0.0.1:${WEB_PORT}"

wait_for_endpoint "API readiness" "${API_URL}/health/ready"
wait_for_endpoint "Web liveness" "${WEB_URL}/health/live"

cat >"${TEMP_DIR}/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Nupack" value="${API_URL}/v3/index.json" allowInsecureConnections="true" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="Nupack">
      <package pattern="TestPackage" />
    </packageSource>
    <packageSource key="nuget.org">
      <!-- Exact package IDs have precedence over the catch-all pattern. -->
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

echo "Publishing TestPackage through the containerized NuGet endpoint."
dotnet nuget push "${ROOT_DIR}/test/TestPackage.1.0.0.nupkg" \
    --source "${API_URL}/v3/index.json" \
    --api-key "${PUBLISH_API_KEY}" \
    --allow-insecure-connections

echo "Restoring with the first empty package cache."
restore_consumer \
    "${TEMP_DIR}/packages-before-restart" \
    "${TEMP_DIR}/http-before-restart" \
    "${TEMP_DIR}/restore-before-restart.log"

echo "Restarting the same container with the same persistent volume."
docker restart "${CONTAINER_NAME}" >/dev/null
wait_for_endpoint "API readiness after restart" "${API_URL}/health/ready"
wait_for_endpoint "Web liveness after restart" "${WEB_URL}/health/live"

echo "Restoring with a second empty package cache after restart."
restore_consumer \
    "${TEMP_DIR}/packages-after-restart" \
    "${TEMP_DIR}/http-after-restart" \
    "${TEMP_DIR}/restore-after-restart.log"

test -f "${TEMP_DIR}/packages-after-restart/testpackage/1.0.0/testpackage.1.0.0.nupkg"
echo "Container smoke passed: package push, restore, restart, and cache-isolated restore succeeded."
