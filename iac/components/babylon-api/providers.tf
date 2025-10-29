terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.5"
    }
  }

  # Configure S3 backend for remote state storage
  # NOTE: The bucket 'babylon-terraform-state-4321' must be created manually before terraform init.
  backend "s3" {
    bucket  = "babylon-terraform-state-4321"
    key     = "terraform.tfstate"
    region  = "eu-west-1"
    encrypt = true
  }
}

# AWS provider configuration
provider "aws" {
  region = var.aws_region
}

