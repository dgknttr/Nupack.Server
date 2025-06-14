# CI/CD Integration Guide

This guide explains how to integrate Nupack Server with various CI/CD platforms for automated package publishing.

## Overview

The CI/CD integration allows you to:
- Automatically build and pack NuGet packages
- Push packages to your private NuGet server
- Manage package versions and releases
- Integrate with your existing development workflow

## GitHub Actions

### Basic Workflow

Create `.github/workflows/nuget-publish.yml`:

```yaml
name: Build and Publish NuGet Package

on:
  push:
    branches: [ main, develop ]
    tags: [ 'v*' ]
  pull_request:
    branches: [ main ]

env:
  NUGET_SERVER_URL: ${{ secrets.NUGET_SERVER_URL }}
  NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0  # Required for GitVersion
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --configuration Release --no-build --verbosity normal
    
    - name: Pack
      run: dotnet pack --configuration Release --no-build --output ./packages
    
    - name: Upload packages as artifacts
      uses: actions/upload-artifact@v4
      with:
        name: nuget-packages
        path: ./packages/*.nupkg

  publish:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v'))
    
    steps:
    - name: Download packages
      uses: actions/download-artifact@v4
      with:
        name: nuget-packages
        path: ./packages
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Add NuGet source
      run: |
        dotnet nuget add source ${{ env.NUGET_SERVER_URL }} \
          --name "Nupack Server" \
          --username "github-actions" \
          --password ${{ env.NUGET_API_KEY }} \
          --store-password-in-clear-text
    
    - name: Push packages
      run: |
        for package in ./packages/*.nupkg; do
          echo "Publishing $package"
          dotnet nuget push "$package" \
            --source "Nupack Server" \
            --api-key ${{ env.NUGET_API_KEY }} \
            --skip-duplicate
        done
```

### Advanced Workflow with Semantic Versioning

```yaml
name: Advanced NuGet Publish

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.gitversion.outputs.semVer }}
      
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0
    
    - name: Install GitVersion
      uses: gittools/actions/gitversion/setup@v0.10.2
      with:
        versionSpec: '5.x'
    
    - name: Determine Version
      id: gitversion
      uses: gittools/actions/gitversion/execute@v0.10.2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: |
        dotnet build --configuration Release \
          -p:Version=${{ steps.gitversion.outputs.semVer }} \
          -p:AssemblyVersion=${{ steps.gitversion.outputs.assemblySemVer }} \
          -p:FileVersion=${{ steps.gitversion.outputs.assemblySemFileVer }}
    
    - name: Test
      run: dotnet test --configuration Release --no-build
    
    - name: Pack
      run: |
        dotnet pack --configuration Release --no-build \
          -p:PackageVersion=${{ steps.gitversion.outputs.semVer }} \
          --output ./packages
    
    - name: Publish to Nupack Server
      if: github.ref == 'refs/heads/main'
      run: |
        dotnet nuget push ./packages/*.nupkg \
          --source ${{ secrets.NUGET_SERVER_URL }} \
          --api-key ${{ secrets.NUGET_API_KEY }} \
          --skip-duplicate
```

## Azure DevOps

### Azure Pipelines YAML

Create `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include:
    - main
    - develop
  tags:
    include:
    - v*

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  nugetServerUrl: $(NUGET_SERVER_URL)

stages:
- stage: Build
  displayName: 'Build and Test'
  jobs:
  - job: BuildJob
    displayName: 'Build Job'
    steps:
    - task: UseDotNet@2
      displayName: 'Use .NET 8.0'
      inputs:
        packageType: 'sdk'
        version: '8.0.x'
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build solution'
      inputs:
        command: 'build'
        arguments: '--configuration $(buildConfiguration) --no-restore'
    
    - task: DotNetCoreCLI@2
      displayName: 'Run tests'
      inputs:
        command: 'test'
        arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage"'
    
    - task: DotNetCoreCLI@2
      displayName: 'Pack NuGet packages'
      inputs:
        command: 'pack'
        packagesToPack: '**/*.csproj'
        configuration: '$(buildConfiguration)'
        outputDir: '$(Build.ArtifactStagingDirectory)/packages'
    
    - task: PublishBuildArtifacts@1
      displayName: 'Publish artifacts'
      inputs:
        pathToPublish: '$(Build.ArtifactStagingDirectory)/packages'
        artifactName: 'nuget-packages'

- stage: Deploy
  displayName: 'Deploy to NuGet Server'
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployJob
    displayName: 'Deploy Job'
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: DownloadBuildArtifacts@0
            displayName: 'Download artifacts'
            inputs:
              buildType: 'current'
              downloadType: 'single'
              artifactName: 'nuget-packages'
              downloadPath: '$(System.ArtifactsDirectory)'
          
          - task: DotNetCoreCLI@2
            displayName: 'Add NuGet source'
            inputs:
              command: 'custom'
              custom: 'nuget'
              arguments: 'add source $(nugetServerUrl) --name "Nupack Server"'
          
          - task: DotNetCoreCLI@2
            displayName: 'Push packages'
            inputs:
              command: 'push'
              packagesToPush: '$(System.ArtifactsDirectory)/nuget-packages/*.nupkg'
              nuGetFeedType: 'external'
              publishFeedCredentials: 'Nupack-Server'
```

## GitLab CI/CD

### GitLab CI Configuration

Create `.gitlab-ci.yml`:

```yaml
stages:
  - build
  - test
  - package
  - deploy

variables:
  DOTNET_VERSION: "8.0"
  NUGET_PACKAGES_DIRECTORY: "packages"

before_script:
  - apt-get update -qq && apt-get install -y -qq git
  - dotnet --version

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet restore
    - dotnet build --configuration Release --no-restore
  artifacts:
    paths:
      - "**/bin/Release/**"
    expire_in: 1 hour

test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage"
  coverage: '/Total\s*\|\s*(\d+(?:\.\d+)?%)/'
  artifacts:
    reports:
      coverage_report:
        coverage_format: cobertura
        path: "**/coverage.cobertura.xml"

package:
  stage: package
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet pack --configuration Release --no-build --output $NUGET_PACKAGES_DIRECTORY
  artifacts:
    paths:
      - $NUGET_PACKAGES_DIRECTORY/*.nupkg
    expire_in: 1 week
  only:
    - main
    - tags

deploy:
  stage: deploy
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet nuget add source $NUGET_SERVER_URL --name "Nupack Server"
    - |
      for package in $NUGET_PACKAGES_DIRECTORY/*.nupkg; do
        echo "Publishing $package"
        dotnet nuget push "$package" \
          --source "Nupack Server" \
          --api-key $NUGET_API_KEY \
          --skip-duplicate
      done
  only:
    - main
    - tags
  when: manual
```

## Jenkins Pipeline

### Jenkinsfile

```groovy
pipeline {
    agent any
    
    environment {
        DOTNET_VERSION = '8.0'
        NUGET_SERVER_URL = credentials('nuget-server-url')
        NUGET_API_KEY = credentials('nuget-api-key')
    }
    
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        
        stage('Setup') {
            steps {
                sh '''
                    # Install .NET SDK if not available
                    if ! command -v dotnet &> /dev/null; then
                        wget https://dot.net/v1/dotnet-install.sh
                        chmod +x dotnet-install.sh
                        ./dotnet-install.sh --version ${DOTNET_VERSION}
                        export PATH="$HOME/.dotnet:$PATH"
                    fi
                    dotnet --version
                '''
            }
        }
        
        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }
        
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release --no-restore'
            }
        }
        
        stage('Test') {
            steps {
                sh 'dotnet test --configuration Release --no-build --logger trx --results-directory TestResults'
            }
            post {
                always {
                    publishTestResults testResultsPattern: 'TestResults/*.trx'
                }
            }
        }
        
        stage('Pack') {
            steps {
                sh 'dotnet pack --configuration Release --no-build --output packages'
            }
            post {
                success {
                    archiveArtifacts artifacts: 'packages/*.nupkg', fingerprint: true
                }
            }
        }
        
        stage('Deploy') {
            when {
                anyOf {
                    branch 'main'
                    buildingTag()
                }
            }
            steps {
                sh '''
                    dotnet nuget add source ${NUGET_SERVER_URL} --name "Nupack Server"
                    
                    for package in packages/*.nupkg; do
                        echo "Publishing $package"
                        dotnet nuget push "$package" \
                            --source "Nupack Server" \
                            --api-key ${NUGET_API_KEY} \
                            --skip-duplicate
                    done
                '''
            }
        }
    }
    
    post {
        always {
            cleanWs()
        }
    }
}
```

## Environment Configuration

### Required Secrets/Variables

For all CI/CD platforms, configure these secrets:

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `NUGET_SERVER_URL` | Your NuGet server API endpoint | `https://nuget.yourcompany.com/v3/index.json` |
| `NUGET_API_KEY` | API key for authentication | `your-api-key-here` |

### GitHub Actions Secrets

1. Go to your repository → Settings → Secrets and variables → Actions
2. Add the required secrets:
   - `NUGET_SERVER_URL`
   - `NUGET_API_KEY`

### Azure DevOps Variables

1. Go to Pipelines → Library → Variable groups
2. Create a new variable group named "NuGet-Config"
3. Add variables:
   - `NUGET_SERVER_URL`
   - `NUGET_API_KEY` (mark as secret)

### GitLab CI Variables

1. Go to Settings → CI/CD → Variables
2. Add variables:
   - `NUGET_SERVER_URL`
   - `NUGET_API_KEY` (mark as protected and masked)

## Best Practices

### Version Management

1. **Semantic Versioning**: Use tools like GitVersion for automatic versioning
2. **Build Numbers**: Include build numbers in pre-release versions
3. **Tag-based Releases**: Use Git tags to trigger release builds

### Security

1. **API Keys**: Store API keys securely in CI/CD secrets
2. **Network Access**: Restrict NuGet server access to CI/CD agents
3. **Audit Logging**: Monitor package publishing activities

### Performance

1. **Caching**: Cache NuGet packages and build artifacts
2. **Parallel Builds**: Use parallel execution where possible
3. **Incremental Builds**: Only build changed projects

### Error Handling

1. **Retry Logic**: Implement retry for network operations
2. **Skip Duplicates**: Use `--skip-duplicate` flag to avoid conflicts
3. **Validation**: Validate packages before publishing

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   ```bash
   # Verify API key
   curl -H "X-NuGet-ApiKey: your-api-key" https://your-server/api/v1/packages
   ```

2. **Network Connectivity**
   ```bash
   # Test server connectivity
   curl -I https://your-server/api/v1/packages
   ```

3. **Package Validation Errors**
   ```bash
   # Validate package locally
   dotnet nuget verify MyPackage.1.0.0.nupkg
   ```

### Debugging Tips

1. **Enable Verbose Logging**:
   ```bash
   dotnet nuget push package.nupkg --source "source" --verbosity detailed
   ```

2. **Check Package Contents**:
   ```bash
   # Extract and inspect .nupkg file
   unzip -l MyPackage.1.0.0.nupkg
   ```

3. **Validate NuSpec**:
   ```bash
   # Check package metadata
   nuget spec MyPackage.csproj
   ```

## Integration Examples

### Multi-Project Solution

For solutions with multiple projects:

```yaml
- name: Pack all projects
  run: |
    for project in src/**/*.csproj; do
      dotnet pack "$project" --configuration Release --output packages
    done

- name: Push all packages
  run: |
    for package in packages/*.nupkg; do
      dotnet nuget push "$package" --source "Nupack Server" --skip-duplicate
    done
```

### Conditional Publishing

```yaml
- name: Publish release packages
  if: startsWith(github.ref, 'refs/tags/v')
  run: |
    dotnet nuget push packages/*.nupkg --source "Nupack Server"

- name: Publish pre-release packages
  if: github.ref == 'refs/heads/develop'
  run: |
    dotnet nuget push packages/*-preview.nupkg --source "Nupack Server"
```

This guide provides comprehensive CI/CD integration options for automating your NuGet package publishing workflow with Nupack Server.
