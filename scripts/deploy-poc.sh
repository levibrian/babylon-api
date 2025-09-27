#!/bin/bash

# Simple POC Deployment Script for Babylon Alfred API
# This script deploys the minimal infrastructure and pushes the Docker image

set -e

echo "🚀 Deploying Babylon Alfred API POC to AWS..."

# Check if AWS CLI is configured
if ! aws sts get-caller-identity &> /dev/null; then
    echo "❌ AWS CLI not configured. Please run 'aws configure' first."
    exit 1
fi

# Get AWS account ID and region
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
AWS_REGION=$(aws configure get region || echo "us-east-1")

echo "📋 AWS Account: $AWS_ACCOUNT_ID"
echo "🌍 AWS Region: $AWS_REGION"

# Deploy infrastructure
echo "🏗️ Deploying infrastructure with Terraform..."
cd infrastructure

# Initialize Terraform
terraform init

# Plan deployment
echo "📋 Planning deployment..."
terraform plan

# Apply deployment
echo "🏗️ Applying infrastructure..."
terraform apply -auto-approve

# Get ECR repository URL
ECR_REPOSITORY=$(terraform output -raw ecr_repository_url)
echo "📦 ECR Repository: $ECR_REPOSITORY"

cd ..

# Build and push Docker image
echo "🐳 Building and pushing Docker image..."

# Login to ECR
echo "🔐 Logging in to ECR..."
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_REPOSITORY

# Build Docker image
echo "🏗️ Building Docker image..."
docker build -t babylon-alfred-api -f src/Babylon.Alfred/Babylon.Alfred.Api/Dockerfile .

# Tag image
echo "🏷️ Tagging image..."
docker tag babylon-alfred-api:latest $ECR_REPOSITORY:latest

# Push image
echo "📤 Pushing image to ECR..."
docker push $ECR_REPOSITORY:latest

# Update ECS service
echo "🔄 Updating ECS service..."
aws ecs update-service \
    --cluster "babylon-alfred-cluster" \
    --service "babylon-alfred-service" \
    --force-new-deployment \
    --region $AWS_REGION

# Get API URL
cd infrastructure
API_URL=$(terraform output -raw api_url)
SWAGGER_URL=$(terraform output -raw swagger_url)

echo ""
echo "✅ Deployment completed!"
echo "🌐 API URL: $API_URL"
echo "📊 Swagger UI: $SWAGGER_URL"
echo "💡 Note: It may take a few minutes for the service to be ready."
echo ""
echo "🔍 To check service status:"
echo "aws ecs describe-services --cluster babylon-alfred-cluster --services babylon-alfred-service"
