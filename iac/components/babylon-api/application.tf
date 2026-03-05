# AWS Service Catalog AppRegistry Application for Babylon Alfred
# This creates a centralized application view in AWS Console for resource tracking,
# cost allocation, and monitoring across all Babylon Alfred infrastructure.

resource "aws_servicecatalogappregistry_application" "babylon_alfred" {
  name        = "babylon-alfred"
  description = "Babylon Alfred - Personal investment portfolio management platform"

  tags = {
    Name        = "babylon-alfred"
    Environment = "production"
    Project     = "babylon-alfred"
    ManagedBy   = "terraform"
    CostCenter  = "engineering"
  }
}

# Resource Group for automatic resource discovery
# This groups all resources tagged with Application=babylon-api
resource "aws_resourcegroups_group" "babylon_alfred" {
  name        = "babylon-alfred-resources"
  description = "All AWS resources associated with the Babylon Alfred application"

  resource_query {
    query = jsonencode({
      ResourceTypeFilters = [
        "AWS::AllSupported"
      ]
      TagFilters = [
        {
          Key    = "Application"
          Values = ["babylon-api"]
        }
      ]
    })
  }

  tags = {
    Name        = "babylon-alfred-resources"
    Environment = "production"
    Project     = "babylon-alfred"
    ManagedBy   = "terraform"
    Application = "babylon-api"
  }
}

# Attribute Group - defines the tags that will be propagated to associated resources
# This is what enables AppRegistry to discover and display resources in the Application dashboard
resource "aws_servicecatalogappregistry_attribute_group" "babylon_alfred" {
  name        = "babylon-alfred-attributes"
  description = "Common attributes and tags for Babylon Alfred application resources"

  attributes = jsonencode({
    Application = "babylon-api"
    Project     = "babylon-alfred"
    Environment = "production"
    ManagedBy   = "terraform"
    CostCenter  = "engineering"
  })

  tags = {
    Name        = "babylon-alfred-attributes"
    Environment = "production"
    Project     = "babylon-alfred"
    ManagedBy   = "terraform"
    Application = "babylon-api"
  }
}

# Associate the Attribute Group with the Application
# This enables tag-based resource discovery in the AppRegistry dashboard
resource "aws_servicecatalogappregistry_attribute_group_association" "babylon_alfred" {
  application_id      = aws_servicecatalogappregistry_application.babylon_alfred.id
  attribute_group_id  = aws_servicecatalogappregistry_attribute_group.babylon_alfred.id
}

# Note: AppRegistry will now automatically discover all resources tagged with Application=babylon-api
# and display them in the myApplications dashboard in the AWS Console.
