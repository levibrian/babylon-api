variable "aws_region" {
  description = "AWS region"
  type        = string
}

variable "aws_account_id" {
  description = "AWS account ID"
  type        = string
}

variable "project_name" {
  description = "Name of the project"
  type        = string
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
}

variable "public_subnet_cidr" {
  description = "CIDR block for public subnet"
  type        = string
}

variable "app_port" {
  description = "Port exposed by the application"
  type        = number
}

variable "fargate_cpu" {
  description = "Fargate instance CPU units to provision"
  type        = number
}

variable "fargate_memory" {
  description = "Fargate instance memory to provision"
  type        = number
}

variable "desired_count" {
  description = "Number of ECS tasks to run"
  type        = number
}
