# DotNETWeekly-Agent

An intelligent ASP.NET Core application that integrates with Large Language Models (LLM) to automate GitHub issue management and newsletter generation for .NET Weekly.

## 🚀 Features

### 1. GitHub Webhook Integration

- **Automatic Issue Summarization**: Receives GitHub webhook notifications when new issues are created
- **AI-Powered Analysis**: Uses LLM to analyze and summarize issue content
- **Automatic Comments**: Posts intelligent summaries back to the GitHub issue as comments
- **Real-time Processing**: Handles webhook events in real-time

### 2. Newsletter Generation

- **Issue Aggregation**: Scans all open GitHub issues across repositories
- **Content Curation**: Uses AI to curate and organize issues into newsletter format
- **Automated Publishing**: Generates and publishes newsletters to designated repository folders
- **Customizable Templates**: Supports different newsletter formats and layouts

### 3. Translation Services

- **Multi-language Support**: Translates newsletters from Chinese to English
- **AI-Powered Translation**: Uses LLM for context-aware, high-quality translations
- **Content Preservation**: Maintains formatting and technical accuracy during translation

## 🛠 Technology Stack

- **Framework**: ASP.NET Core 9.0
- **Language**: C# / TypeScript
- **LLM Integration**: OpenAI API / Azure OpenAI
- **GitHub Integration**: GitHub REST API & Webhooks & MCP Server
- **Authentication**: Personal Access Tokens
- **Hosting**: Azure App Service

## 📋 Prerequisites

- .NET 9.0 SDK
- GitHub Personal Access Toke
- LLM API credentials (OpenAI, Azure OpenAI, etc.)
- Visual Studio 2022 or VS Code


## 🔒 Security Considerations

- **Webhook Verification**: All GitHub webhooks are verified using HMAC-SHA256
- **API Rate Limiting**: Implements rate limiting for GitHub API calls
- **Secure Configuration**: Sensitive data stored in environment variables
- **Input Validation**: All inputs are validated and sanitized

## 📝 Development Roadmap

See [Work Breakdown](#work-breakdown) section below for detailed development phases.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

# Work Breakdown

## Phase 1: Core Infrastructure & GitHub Integration (Week 1-2)

### 1.1 Project Setup & Configuration
·
- [ ] **Update project dependencies**
  - Add GitHub API client
  - Add HTTP client for LLM APIs
  - Add configuration management packages
  - Add logging framework

- [ ] **Configuration Management**
  - Create `appsettings.json` structure for all required settings
  - Implement strongly-typed configuration classes
  - Add environment variable support
  - Create `appsettings.example.json` template

- [ ] **Base Infrastructure**
  - Set up dependency injection container
  - Configure logging
  - Add error handling middleware
  - Set up CORS policies

### 1.2 GitHub Integration Foundation

- [ ] **GitHub Service Layer**
  - Create `IGitHubService` interface
  - Implement `GitHubService` class
  - Add methods for:
    - Authenticating with GitHub API
    - Fetching repository issues
    - Adding comments to issues
    - Creating/updating files in repositories

- [ ] **GitHub Models**
  - Create `GitHubWebhookPayload.cs` for webhook data
  - Create `IssueModel.cs` for issue representation
  - Create `CommentModel.cs` for comment data
  - Add JSON serialization attributes

### 1.3 Webhook Infrastructure

- [ ] **Webhook Controller**
  - Create `WebhookController.cs`
  - Implement webhook signature verification
  - Add webhook payload parsing

- [ ] **Webhook Security**
  - Implement HMAC-SHA256 verification
  - Add IP whitelist validation (optional)
  - Create middleware for webhook authentication

## Phase 2: LLM Integration & Issue Processing (Week 2-3)

### 2.1 LLM Service Layer

- [ ] **LLM Service Interface**
  - Create `ILLMService` interface
  - Define methods for:
    - Issue summarization
    - Content translation
    - Newsletter generation

- [ ] **LLM Service Implementation**
  - Implement `LLMService` class
  - Add support for OpenAI API
  - Add support for Azure OpenAI
  - Implement retry logic and error handling
  - Add token counting and cost tracking

### 2.2 Issue Processing Logic

- [ ] **Issue Analysis**
  - Create prompt templates for issue summarization
  - Implement issue content preprocessing
  - Add context extraction (labels, assignees, etc.)
  - Create summary generation logic

- [ ] **Comment Management**
  - Implement automatic comment posting
  - Add comment formatting templates
  - Create duplicate comment prevention
  - Add comment update functionality

### 2.3 Background Processing

- [ ] **Async Processing**
  - Implement background job processing
  - Add queue management for webhook events
  - Create retry mechanisms for failed operations
  - Add processing status tracking

## Phase 3: Newsletter Generation System (Week 3-4)

### 3.1 Newsletter Service

- [ ] **Newsletter Service Interface**
  - Define methods for:
    - Issue aggregation
    - Content curation
    - Newsletter formatting
    - Publishing

- [ ] **Newsletter Service Implementation**
  - Implement issue scanning across repositories
  - Add content categorization logic
  - Create newsletter template system
  - Implement markdown/HTML generation

### 3.2 Newsletter Templates

- [ ] **Template System**
  - Create base newsletter template
  - Add categorization templates (bugs, features, discussions)
  - Implement dynamic content insertion
  - Add customizable formatting options

- [ ] **Content Curation**
  - Implement AI-powered content selection
  - Add duplicate detection
  - Create relevance scoring
  - Implement content summarization

### 3.3 Publishing System

- [ ] **Repository Publishing**
  - Implement file creation in GitHub repositories
  - Add versioning and archiving
  - Create publication workflows
  - Add publishing history tracking

## Phase 4: Translation Services (Week 4-5)

### 4.1 Translation Infrastructure

- [ ] **Translation Service**
  - Extend `LLMService` with translation capabilities
  - Create translation prompt templates
  - Implement language detection
  - Add translation quality validation

- [ ] **Multi-language Support**
  - Create language configuration system
  - Add support for Chinese to English translation
  - Implement format preservation during translation
  - Add technical term handling

### 4.2 Translation Workflows

- [ ] **Batch Translation**
  - Implement newsletter translation workflows
  - Add progress tracking for large documents
  - Create translation caching
  - Add human review integration points

## Phase 5: Web UI & Management Interface (Week 5-6)

### 5.1 Dashboard Development

- [ ] **Main Dashboard**
  - Create responsive dashboard layout
  - Add recent activity display
  - Implement status monitoring
  - Add quick action buttons

- [ ] **Issue Management Interface**
  - Create issue list view
  - Add filtering and searching
  - Implement bulk operations
  - Add processing status indicators

### 5.2 Newsletter Management UI

- [ ] **Newsletter Interface**
  - Create newsletter preview functionality
  - Add editing capabilities
  - Implement publication scheduling
  - Add translation management

- [ ] **Configuration UI**
  - Create settings management interface
  - Add repository configuration
  - Implement webhook management
  - Add API key management

### 5.3 Monitoring & Analytics

- [ ] **Activity Monitoring**
  - Add processing statistics
  - Implement error tracking
  - Create usage analytics
  - Add performance metrics

## Phase 6: Testing, Documentation & Deployment (Week 6-7)

### 6.1 Testing Suite

- [ ] **Unit Tests**
  - Test all service classes
  - Test webhook processing
  - Test LLM integration
  - Test GitHub API interactions

- [ ] **Integration Tests**
  - Test end-to-end workflows
  - Test webhook to comment flow
  - Test newsletter generation
  - Test translation pipeline

### 6.2 Documentation

- [ ] **API Documentation**
  - Document all endpoints
  - Add request/response examples
  - Create webhook setup guide
  - Add troubleshooting guide

- [ ] **Deployment Documentation**
  - Create Docker deployment guide
  - Add Azure App Service instructions
  - Document environment setup
  - Add monitoring setup guide

### 6.3 Production Readiness

- [ ] **Performance Optimization**
  - Implement caching strategies
  - Add connection pooling
  - Optimize database queries (if applicable)
  - Add performance monitoring

- [ ] **Security Hardening**
  - Security audit and penetration testing
  - Add rate limiting
  - Implement request validation
  - Add security headers

- [ ] **Deployment & CI/CD**
  - Set up GitHub Actions workflow
  - Create Docker containers
  - Configure production environment
  - Set up monitoring and alerting