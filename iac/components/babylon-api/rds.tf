# DB Subnet Group (Required even if only one subnet is used)
resource "aws_db_subnet_group" "rds_subnet_group" {
  name       = "babylon-rds-subnet-group"
  subnet_ids = [aws_subnet.public_a.id, aws_subnet.public_b.id]

  tags = {
    Name = "babylon-rds-subnet-group"
  }
}

# The RDS PostgreSQL Instance
resource "aws_db_instance" "main_db" {
  identifier           = "babylon-postgres-db"
  engine               = "postgres"
  engine_version       = "17.2"
  instance_class       = var.db_instance_type
  allocated_storage    = 20
  storage_type         = "gp2"
  db_name              = "babylon_dev"
  username             = var.db_username
  password             = random_password.db_password.result
  port                 = 5432
  publicly_accessible  = true # Required for local connection via public endpoint
  skip_final_snapshot  = true
  db_subnet_group_name = aws_db_subnet_group.rds_subnet_group.name

  vpc_security_group_ids = [aws_security_group.rds_sg.id]
}

