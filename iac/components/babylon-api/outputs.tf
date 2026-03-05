output "db_connection_string" {
  description = "Database connection string template (requires manual ARN resolution for password)"
  value = format(
    "postgresql://%s:[PASSWORD_FROM_SECRET]@%s:%s/%s",
    aws_db_instance.main_db.username,
    aws_db_instance.main_db.address,
    aws_db_instance.main_db.port,
    aws_db_instance.main_db.db_name
  )
}

output "db_secret_arn" {
  description = "ARN of the Secrets Manager secret holding the master password."
  value       = aws_secretsmanager_secret.db_secret.arn
}

output "rds_endpoint_address" {
  description = "The DNS endpoint for the RDS instance."
  value       = aws_db_instance.main_db.address
}

output "application_arn" {
  description = "ARN of the AWS Service Catalog AppRegistry Application for Babylon Alfred."
  value       = aws_servicecatalogappregistry_application.babylon_alfred.arn
}

output "application_id" {
  description = "ID of the AWS Service Catalog AppRegistry Application for Babylon Alfred."
  value       = aws_servicecatalogappregistry_application.babylon_alfred.id
}

output "resource_group_arn" {
  description = "ARN of the Resource Group containing all Babylon Alfred resources."
  value       = aws_resourcegroups_group.babylon_alfred.arn
}

output "attribute_group_arn" {
  description = "ARN of the AppRegistry Attribute Group for tag propagation and resource discovery."
  value       = aws_servicecatalogappregistry_attribute_group.babylon_alfred.arn
}

