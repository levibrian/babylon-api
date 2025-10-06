module "infrastructure" {
  source = "../../"

  aws_region        = var.aws_region
  aws_account_id    = var.aws_account_id
  project_name      = var.project_name
  vpc_cidr          = var.vpc_cidr
  public_subnet_cidr = var.public_subnet_cidr
  app_port          = var.app_port
  fargate_cpu       = var.fargate_cpu
  fargate_memory    = var.fargate_memory
  desired_count     = var.desired_count
}
