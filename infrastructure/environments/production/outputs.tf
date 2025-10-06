output "alb_dns_name" {
  description = "DNS name of the load balancer"
  value       = module.infrastructure.alb_dns_name
}

output "api_url" {
  description = "URL of the API"
  value       = module.infrastructure.api_url
}

output "swagger_url" {
  description = "Swagger UI URL"
  value       = module.infrastructure.swagger_url
}

output "ecr_repository_url" {
  description = "URL of the ECR repository"
  value       = module.infrastructure.ecr_repository_url
}

output "ecs_cluster_name" {
  description = "Name of the ECS cluster"
  value       = module.infrastructure.ecs_cluster_name
}

output "ecs_service_name" {
  description = "Name of the ECS service"
  value       = module.infrastructure.ecs_service_name
}
