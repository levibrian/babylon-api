variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "eu-west-1"
}

variable "aws_account_id" {
  description = "AWS account ID"
  type        = string
  default     = "551647579881"
}

variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "babylon-alfred"
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidr" {
  description = "CIDR block for public subnet"
  type        = string
  default     = "10.0.1.0/24"
}

variable "app_port" {
  description = "Port exposed by the application"
  type        = number
  default     = 80
}

variable "fargate_cpu" {
  description = "Fargate instance CPU units to provision"
  type        = number
  default     = 256
}

variable "fargate_memory" {
  description = "Fargate instance memory to provision"
  type        = number
  default     = 512
}

variable "desired_count" {
  description = "Number of ECS tasks to run"
  type        = number
  default     = 1
}