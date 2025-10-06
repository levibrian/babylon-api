#!/bin/bash

# Setup script for Babylon Alfred API
# This script sets up the development environment and initializes Terraform

set -e

echo "🚀 Setting up Babylon Alfred API..."

# Check if required tools are installed
echo "🔍 Checking prerequisites..."

if ! command -v aws &> /dev/null; then
    echo "❌ AWS CLI not found. Please install AWS CLI first."
    exit 1
fi

if ! command -v terraform &> /dev/null; then
    echo "❌ Terraform not found. Please install Terraform first."
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Please install Docker first."
    exit 1
fi

echo "✅ All prerequisites found!"

# Configure AWS credentials
echo "🔐 Please configure your AWS credentials:"
aws configure

# Create ECR repositories
echo "📦 Creating ECR repositories..."

for env in dev prod; do
    echo "Creating repository for $env environment..."
    aws ecr create-repository \
        --repository-name "babylon-alfred-$env" \
        --region us-east-1 \
        --image-scanning-configuration scanOnPush=true \
        --image-tag-mutability MUTABLE || echo "Repository already exists"
done

# Initialize Terraform for dev environment
echo "🏗️ Initializing Terraform for dev environment..."
cd infrastructure/environments/dev
terraform init

# Initialize Terraform for prod environment
echo "🏗️ Initializing Terraform for prod environment..."
cd ../prod
terraform init

cd ../../..

echo "✅ Setup completed!"
echo ""
echo "Next steps:"
echo "1. Update database passwords in terraform.tfvars files"
echo "2. Run: ./scripts/deploy.sh dev plan"
echo "3. Run: ./scripts/deploy.sh dev apply"
echo "4. Run: ./scripts/build-and-push.sh dev"
