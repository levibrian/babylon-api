# 🚀 POC Deployment Guide

This is a minimal AWS deployment setup for the Babylon Alfred API POC. It's designed to be cost-effective and stay within AWS Free Tier limits.

## 💰 **Cost-Effective Design**

This setup uses only AWS Free Tier resources:
- **ECS Fargate**: 20 GB-hours per month (free)
- **Application Load Balancer**: 750 hours per month (free)
- **ECR**: 500 MB storage per month (free)
- **CloudWatch Logs**: 5 GB per month (free)
- **VPC**: Always free

## 🏗️ **What Gets Created**

- **VPC** with single public subnet
- **Application Load Balancer** for HTTP traffic
- **ECS Fargate** cluster with 1 task
- **ECR** repository for Docker images
- **CloudWatch** log group
- **IAM** roles for ECS

## 🚀 **Quick Start**

### Prerequisites
- AWS CLI configured (`aws configure`)
- Docker installed
- Terraform installed

### Deploy Everything
```bash
# One command deployment
./scripts/deploy-poc.sh
```

### Destroy Everything
```bash
# Clean up when done
./scripts/destroy-poc.sh
```

## 📋 **Manual Steps**

If you prefer to run steps manually:

### 1. Deploy Infrastructure
```bash
cd infrastructure
terraform init
terraform plan
terraform apply
```

### 2. Build and Push Docker Image
```bash
# Get ECR repository URL
ECR_REPO=$(cd infrastructure && terraform output -raw ecr_repository_url)

# Login to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin $ECR_REPO

# Build and push
docker build -t babylon-alfred-api -f src/Babylon.Alfred/Babylon.Alfred.Api/Dockerfile .
docker tag babylon-alfred-api:latest $ECR_REPO:latest
docker push $ECR_REPO:latest
```

### 3. Update ECS Service
```bash
aws ecs update-service --cluster babylon-alfred-cluster --service babylon-alfred-service --force-new-deployment
```

## 🌐 **Access Your API**

After deployment, you'll get:
- **API URL**: `http://<alb-dns-name>`
- **Swagger UI**: `http://<alb-dns-name>/swagger`
- **Health Check**: `http://<alb-dns-name>/health`

## 🔍 **Monitoring**

### Check Service Status
```bash
aws ecs describe-services --cluster babylon-alfred-cluster --services babylon-alfred-service
```

### View Logs
```bash
aws logs tail /ecs/babylon-alfred --follow
```

### Check ALB Health
```bash
aws elbv2 describe-target-health --target-group-arn $(cd infrastructure && terraform output -raw target_group_arn)
```

## ⚠️ **Important Notes**

1. **No Database**: This POC uses in-memory storage (data is lost on restart)
2. **HTTP Only**: No SSL certificate (for simplicity)
3. **Single AZ**: Not highly available (for cost savings)
4. **No Persistence**: All data is temporary

## 🔄 **Next Steps**

Once you're happy with the POC, you can:
1. Add RDS database for persistence
2. Add SSL certificate for HTTPS
3. Add private subnets for security
4. Add auto-scaling
5. Add monitoring and alerts

## 🛠️ **Troubleshooting**

### Service Not Starting
- Check CloudWatch logs: `aws logs tail /ecs/babylon-alfred --follow`
- Verify security group rules
- Check task definition

### ALB Health Check Failing
- Ensure `/health` endpoint is working
- Check security group allows port 80
- Verify ECS task is running

### Can't Access API
- Check ALB DNS name: `cd infrastructure && terraform output alb_dns_name`
- Verify ECS service is running
- Check security group rules

## 💡 **Tips**

- Use `terraform plan` before `terraform apply` to see what will be created
- Monitor AWS costs in the AWS Console
- Keep the infrastructure simple for POC phase
- Destroy resources when not in use to avoid charges
