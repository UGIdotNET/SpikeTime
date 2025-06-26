terraform {
  
}

provider "aws" {
  region = "eu-west-1"
}

resource "aws_ecr_repository" "spiketime_app" {
    name = "spiketime-terraform-app"
}

output "repository_url" {
  value = aws_ecr_repository.spiketime_app.repository_url
}

resource "aws_iam_role" "apprunner_ecr_access" { 
  name = "apprunner-ecr-access-role" 
  assume_role_policy = jsonencode({ 
    Version = "2012-10-17" 
    Statement = [{ 
      Effect = "Allow", 
      Principal = { Service = "build.apprunner.amazonaws.com" } 
      Action = "sts:AssumeRole" 
    }] 
  }) 
}

resource "aws_iam_role_policy" "ecr_pull" {
  name = "ECRPullPolicy"
  role = aws_iam_role.apprunner_ecr_access.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ecr:GetAuthorizationToken",
          "ecr:BatchCheckLayerAvailability",
          "ecr:GetDownloadUrlForLayer",
          "ecr:BatchGetImage"
        ]
        Resource = "*"
      }
    ]
  })
}

resource "aws_apprunner_service" "spiktime_runner" {
  service_name = "spiktime-terraform-runner"
  source_configuration {
    authentication_configuration {
      access_role_arn = aws_iam_role.apprunner_ecr_access.arn
    }
    image_repository {
      image_identifier = "${aws_ecr_repository.spiketime_app.repository_url}:latest"
      image_repository_type = "ECR"
      image_configuration {
        port = "8080"
      }
    }
  }
}

output "service_url" {
  value = aws_apprunner_service.spiktime_runner.service_url
}