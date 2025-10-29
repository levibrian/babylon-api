variable "aws_region" {
  description = "The AWS region to deploy the infrastructure."
  type        = string
  default     = "eu-west-1"
}

variable "vpc_cidr" {
  description = "The CIDR block for the VPC."
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidr_a" {
  description = "CIDR block for the public subnet a needed for local access."
  type        = string
  default     = "10.0.1.0/24"
}

variable "public_subnet_cidr_b" {
  description = "CIDR block for the public subnet b needed for local access."
  type        = string
  default     = "10.0.2.0/24"
}

variable "db_instance_type" {
  description = "The size/class of the RDS DB instance."
  type        = string
  default     = "db.t3.micro"
}

variable "db_username" {
  description = "Master database username."
  type        = string
  default     = "babylonadmin"
}

variable "local_ip_cidr" {
  description = "Your local public IP address in CIDR notation (e.g., 12.34.56.78/32) for secure connection."
  type        = string
  sensitive   = false
}

