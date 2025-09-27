#!/bin/bash

# Babylon Alfred API Deployment Script
# Usage: ./scripts/deploy.sh [dev|prod] [plan|apply|destroy]

set -e

ENVIRONMENT=${1:-dev}
ACTION=${2:-plan}

if [[ "$ENVIRONMENT" != "dev" && "$ENVIRONMENT" != "prod" ]]; then
    echo "Error: Environment must be 'dev' or 'prod'"
    exit 1
fi

if [[ "$ACTION" != "plan" && "$ACTION" != "apply" && "$ACTION" != "destroy" ]]; then
    echo "Error: Action must be 'plan', 'apply', or 'destroy'"
    exit 1
fi

echo "🚀 Deploying Babylon Alfred API to $ENVIRONMENT environment..."

# Change to infrastructure directory
cd infrastructure/environments/$ENVIRONMENT

# Initialize Terraform
echo "📦 Initializing Terraform..."
terraform init

# Validate Terraform configuration
echo "✅ Validating Terraform configuration..."
terraform validate

# Plan or apply
if [[ "$ACTION" == "plan" ]]; then
    echo "📋 Creating Terraform plan..."
    terraform plan -var-file="terraform.tfvars"
elif [[ "$ACTION" == "apply" ]]; then
    echo "🏗️ Applying Terraform configuration..."
    terraform apply -var-file="terraform.tfvars" -auto-approve
    
    echo "✅ Deployment completed!"
    echo "🌐 API URL: $(terraform output -raw api_url)"
    echo "📊 Swagger UI: $(terraform output -raw swagger_url)"
elif [[ "$ACTION" == "destroy" ]]; then
    echo "⚠️ Destroying infrastructure..."
    terraform destroy -var-file="terraform.tfvars" -auto-approve
    echo "🗑️ Infrastructure destroyed!"
fi

echo "✨ Done!"
