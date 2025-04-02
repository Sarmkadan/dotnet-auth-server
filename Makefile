# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build run test clean docker-build docker-up docker-down restore publish

# Colors
CYAN := \033[0;36m
GREEN := \033[0;32m
YELLOW := \033[0;33m
NC := \033[0m

help: ## Show this help message
	@echo "$(CYAN)dotnet-auth-server - Makefile Commands$(NC)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "$(CYAN)%-20s$(NC) %s\n", $$1, $$2}'

# Development
restore: ## Restore NuGet packages
	@echo "$(YELLOW)Restoring packages...$(NC)"
	@dotnet restore

build: restore ## Build the project
	@echo "$(YELLOW)Building project...$(NC)"
	@dotnet build

run: ## Run the project locally (https://localhost:7001)
	@echo "$(YELLOW)Starting server...$(NC)"
	@dotnet run

dev: ## Run with watch for file changes
	@echo "$(YELLOW)Starting in development mode...$(NC)"
	@dotnet watch run

# Testing
test: ## Run all unit tests
	@echo "$(YELLOW)Running tests...$(NC)"
	@dotnet test

test-verbose: ## Run tests with verbose output
	@echo "$(YELLOW)Running tests (verbose)...$(NC)"
	@dotnet test --verbosity detailed

test-coverage: ## Run tests with code coverage
	@echo "$(YELLOW)Running tests with coverage...$(NC)"
	@dotnet test /p:CollectCoverage=true /p:CoverageFormat=json

# Code Quality
format: ## Format code with dotnet format
	@echo "$(YELLOW)Formatting code...$(NC)"
	@dotnet format

lint: ## Run code analysis
	@echo "$(YELLOW)Running code analysis...$(NC)"
	@dotnet analyzers

# Publishing
publish: ## Publish in Release mode
	@echo "$(YELLOW)Publishing...$(NC)"
	@dotnet publish -c Release -o ./publish

publish-self-contained-linux: ## Publish as self-contained for Linux
	@echo "$(YELLOW)Publishing self-contained (Linux)...$(NC)"
	@dotnet publish -c Release -r linux-x64 --self-contained -o ./publish-linux

publish-self-contained-win: ## Publish as self-contained for Windows
	@echo "$(YELLOW)Publishing self-contained (Windows)...$(NC)"
	@dotnet publish -c Release -r win-x64 --self-contained -o ./publish-win

# Docker
docker-build: ## Build Docker image
	@echo "$(YELLOW)Building Docker image...$(NC)"
	@docker build -t dotnet-auth-server:latest .

docker-build-verbose: ## Build Docker image with verbose output
	@echo "$(YELLOW)Building Docker image (verbose)...$(NC)"
	@docker build --progress=plain -t dotnet-auth-server:latest .

docker-up: ## Start Docker Compose stack
	@echo "$(YELLOW)Starting Docker Compose...$(NC)"
	@docker-compose up -d
	@echo "$(GREEN)✓ Services started$(NC)"
	@echo "  Auth Server: https://localhost:5001"
	@echo "  PostgreSQL: localhost:5432"
	@echo "  Redis: localhost:6379"
	@echo "  Adminer: http://localhost:8081"
	@echo "  pgAdmin: http://localhost:5050"
	@echo "  Grafana: http://localhost:3000"

docker-down: ## Stop Docker Compose stack
	@echo "$(YELLOW)Stopping Docker Compose...$(NC)"
	@docker-compose down

docker-logs: ## View Docker logs
	@docker-compose logs -f auth-server

docker-logs-all: ## View all Docker logs
	@docker-compose logs -f

docker-clean: ## Clean all Docker data
	@echo "$(YELLOW)Cleaning Docker volumes...$(NC)"
	@docker-compose down -v
	@echo "$(GREEN)✓ Docker cleaned$(NC)"

docker-shell: ## Access application container shell
	@docker exec -it dotnet-auth-server /bin/sh

docker-db-shell: ## Access database container shell
	@docker exec -it dotnet-auth-db psql -U auth_user -d authdb

# Code Analysis & Quality
sonarqube: ## Run SonarQube analysis
	@echo "$(YELLOW)Running SonarQube analysis...$(NC)"
	@dotnet sonarscanner begin /k:"dotnet-auth-server"
	@dotnet build
	@dotnet sonarscanner end

# Documentation
docs-serve: ## Serve documentation locally
	@echo "$(YELLOW)Serving documentation...$(NC)"
	@echo "Open browser to http://localhost:8000"
	@python3 -m http.server 8000 --directory ./docs

docs-generate: ## Generate documentation
	@echo "$(YELLOW)Generating documentation...$(NC)"
	@docfx build docs/docfx.json

# Environment
env-setup: ## Set up development environment
	@echo "$(YELLOW)Setting up environment...$(NC)"
	@dotnet tool install -g dotnet-format || true
	@dotnet tool install -g dotnet-sonarscanner || true
	@echo "$(GREEN)✓ Environment ready$(NC)"

# Git
git-commit-main: ## Commit changes to main branch
	@git add -A
	@git commit -m "Update: Phase 3 - Documentation, Examples & Polish"
	@git push origin main

# Database
db-migrate: ## Run database migrations
	@echo "$(YELLOW)Running migrations...$(NC)"
	@dotnet ef database update

db-migrate-add: ## Add new migration
	@echo "$(YELLOW)Creating migration...$(NC)"
	@read -p "Enter migration name: " name; \
	dotnet ef migrations add $$name

db-reset: ## Reset database (warning: destructive)
	@echo "$(YELLOW)Resetting database...$(NC)"
	@dotnet ef database drop --force
	@dotnet ef database update

# Cleanup
clean: ## Clean build artifacts
	@echo "$(YELLOW)Cleaning...$(NC)"
	@rm -rf bin obj publish publish-* *.trx
	@dotnet clean
	@echo "$(GREEN)✓ Cleaned$(NC)"

clean-all: clean docker-clean ## Complete cleanup
	@echo "$(GREEN)✓ Complete cleanup done$(NC)"

# Utilities
swagger-generate: ## Generate OpenAPI/Swagger documentation
	@echo "$(YELLOW)Generating Swagger...$(NC)"
	@dotnet build
	@echo "$(GREEN)✓ Visit https://localhost:7001/swagger$(NC)"

security-scan: ## Run security vulnerability scan
	@echo "$(YELLOW)Running security scan...$(NC)"
	@dotnet list package --vulnerable

version: ## Show version information
	@echo "$(CYAN)dotnet-auth-server$(NC)"
	@dotnet --version
	@echo ""
	@grep -A1 "Version" dotnet-auth-server.csproj | grep -v "^--"

info: ## Show project information
	@echo "$(CYAN)Project Information$(NC)"
	@echo "  Name: dotnet-auth-server"
	@echo "  Author: Vladyslav Zaiets"
	@echo "  License: MIT"
	@echo "  .NET Target: net10.0"
	@echo ""
	@echo "$(CYAN)Quick Commands$(NC)"
	@echo "  make build        - Build project"
	@echo "  make run          - Run locally"
	@echo "  make test         - Run tests"
	@echo "  make docker-up    - Start Docker stack"
	@echo "  make clean        - Clean build artifacts"
	@echo ""
	@echo "See 'make help' for all commands"

.DEFAULT_GOAL := help
