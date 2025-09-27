#!/bin/bash

# Destroy POC Infrastructure
# This script destroys all AWS resources created for the POC

set -e

echo "⚠️ Destroying Babylon Alfred API POC infrastructure..."

# Confirm destruction
read -p "Are you sure you want to destroy all infrastructure? (yes/no): " confirm
if [[ $confirm != "yes" ]]; then
    echo "❌ Destruction cancelled."
    exit 1
fi

# Destroy infrastructure
echo "🗑️ Destroying infrastructure with Terraform..."
cd infrastructure

# Destroy infrastructure
terraform destroy -auto-approve

echo "✅ Infrastructure destroyed!"
echo "💡 Note: ECR repository and images may still exist. Delete them manually if needed."
