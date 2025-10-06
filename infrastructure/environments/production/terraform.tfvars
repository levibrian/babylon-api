# Production Environment Configuration

aws_region = "eu-west-1"
aws_account_id = "551647579881"
project_name = "babylon-alfred"

# VPC Configuration
vpc_cidr = "10.0.0.0/16"
public_subnet_cidr = "10.0.1.0/24"

# Application Configuration
app_port = 80
fargate_cpu = 512
fargate_memory = 1024
desired_count = 2
