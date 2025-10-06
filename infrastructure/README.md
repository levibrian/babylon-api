# 🏗️ Infrastructure Modules

This directory contains the modularized Terraform configuration for the Babylon Alfred API POC.

## 📁 Structure

```
infrastructure/
├── main.tf                    # Main configuration using modules
├── variables.tf               # Variable definitions
├── outputs.tf                 # Output definitions
├── providers.tf               # Provider configurations & version requirements
├── terraform.tfvars           # Default variables (for reference)
├── modules/                   # Reusable modules
│   ├── vpc/                   # VPC and networking
│   ├── alb/                   # Application Load Balancer
│   ├── ecs/                   # ECS Fargate cluster
│   └── iam/                   # IAM roles and policies
└── environments/              # Environment-specific configurations
    ├── development/           # Development environment
    │   ├── main.tf
    │   ├── variables.tf
    │   ├── outputs.tf
    │   └── terraform.tfvars
    └── production/            # Production environment
        ├── main.tf
        ├── variables.tf
        ├── outputs.tf
        └── terraform.tfvars
```

## 🚀 Quick Start

### Development Environment
```bash
# Deploy to development
./scripts/deploy-dev.sh
```

### Production Environment
```bash
# Deploy to production
./scripts/deploy-prod.sh
```

### Manual Deployment
```bash
# Navigate to environment
cd infrastructure/environments/development

# Deploy infrastructure
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply -var-file="terraform.tfvars"
```

## 📋 File Organization

### Root Level Files
- **`main.tf`** - Main configuration using modules
- **`variables.tf`** - All variable definitions
- **`outputs.tf`** - All output definitions
- **`providers.tf`** - Provider configurations & version requirements

### Modules
Each module is self-contained with its own:
- **`main.tf`** - Resource definitions
- **`variables.tf`** - Input variables
- **`outputs.tf`** - Output values

## 📋 Modules Overview

### VPC Module
- Creates VPC with single public subnet
- Internet Gateway for internet access
- Route table and associations

### ALB Module
- Application Load Balancer
- Target group with health checks
- Security group for ALB
- HTTP listener

### ECS Module
- ECS Fargate cluster
- ECS task definition
- ECS service
- ECR repository
- CloudWatch log group
- Security group for ECS tasks

### IAM Module
- ECS task execution role
- Required policy attachments

## 🔧 Configuration

All configuration is done through variables in `terraform.tfvars`. See `terraform.tfvars.example` for available options.

## 💰 Cost

This setup uses only AWS Free Tier resources:
- ECS Fargate: 20 GB-hours/month
- ALB: 750 hours/month
- ECR: 500 MB storage/month
- CloudWatch Logs: 5 GB/month
- VPC: Always free

## 🗑️ Cleanup

To destroy all resources:
```bash
terraform destroy
```
