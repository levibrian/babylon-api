#!/bin/bash

# Build and push Docker image to ECR
# Usage: ./scripts/build-and-push.sh [dev|prod]

set -e

ENVIRONMENT=${1:-dev}

if [[ "$ENVIRONMENT" != "dev" && "$ENVIRONMENT" != "prod" ]]; then
    echo "Error: Environment must be 'dev' or 'prod'"
    exit 1
fi

echo "🐳 Building and pushing Docker image for $ENVIRONMENT environment..."

# Get AWS account ID and region
AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
AWS_REGION=$(aws configure get region)

if [[ -z "$AWS_ACCOUNT_ID" || -z "$AWS_REGION" ]]; then
    echo "Error: AWS credentials not configured"
    exit 1
fi

# ECR repository URL
ECR_REPOSITORY="$AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/babylon-alfred-$ENVIRONMENT"

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

echo "✅ Image pushed successfully!"
echo "🔄 Updating ECS service..."

# Update ECS service to force new deployment
aws ecs update-service \
    --cluster "babylon-alfred-$ENVIRONMENT-cluster" \
    --service "babylon-alfred-$ENVIRONMENT-service" \
    --force-new-deployment \
    --region $AWS_REGION

echo "✨ Deployment initiated!"
