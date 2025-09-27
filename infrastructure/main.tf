# VPC Module
module "vpc" {
  source = "./modules/vpc"
  
  project_name      = var.project_name
  vpc_cidr          = var.vpc_cidr
  public_subnet_cidr = var.public_subnet_cidr
  availability_zone = "eu-west-1a"
}

# IAM Module
module "iam" {
  source = "./modules/iam"
  
  project_name = var.project_name
}

# ALB Module
module "alb" {
  source = "./modules/alb"
  
  project_name = var.project_name
  vpc_id       = module.vpc.vpc_id
  subnet_id    = module.vpc.public_subnet_id
  app_port     = var.app_port
}

# ECS Module
module "ecs" {
  source = "./modules/ecs"
  
  project_name                = var.project_name
  vpc_id                     = module.vpc.vpc_id
  subnet_id                  = module.vpc.public_subnet_id
  target_group_arn           = module.alb.target_group_arn
  alb_security_group_id      = module.alb.security_group_id
  alb_listener_arn           = module.alb.alb_listener_arn
  ecs_task_execution_role_arn = module.iam.ecs_task_execution_role_arn
  app_port                   = var.app_port
  fargate_cpu                = var.fargate_cpu
  fargate_memory             = var.fargate_memory
  desired_count              = var.desired_count
  aws_region                 = var.aws_region
  aws_account_id             = var.aws_account_id
}