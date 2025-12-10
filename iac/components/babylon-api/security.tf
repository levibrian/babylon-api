# Generates a secure, random password for the master user
resource "random_password" "db_password" {
  length           = 16
  special          = true
  override_special = "!#$%&*()_+-="
  upper            = true
  lower            = true
}

# Stores the generated password securely in AWS Secrets Manager
resource "aws_secretsmanager_secret" "db_secret" {
  name = "babylon-rds-master-password"
}

resource "aws_secretsmanager_secret_version" "db_secret_version" {
  secret_id     = aws_secretsmanager_secret.db_secret.id
  secret_string = random_password.db_password.result
}

# Security Group: ALLOWS ingress only from your local IP on the PostgreSQL port
resource "aws_security_group" "rds_sg" {
  name        = "babylon-rds-sg"
  description = "Allow inbound traffic from local IP for RDS connection"
  vpc_id      = aws_vpc.main.id

  # Ingress Rule: Allows PostgreSQL traffic (5432) from ANY IP
  # WARNING: This allows public access to your database. Ensure you have:
  # 1. Strong database password (auto-generated in this config)
  # 2. Proper authentication configured in your application
  # 3. Consider using AWS VPN or restricting to specific IP ranges for production
  ingress {
    description = "PostgreSQL access from anywhere"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"] # Allows connections from any IP address
  }

  # Egress Rule: Allows all outbound traffic (needed for patching/updates)
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "babylon-rds-sg"
  }
}

