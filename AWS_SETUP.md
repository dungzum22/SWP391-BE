# AWS S3 Configuration Setup

## Development Environment

1. Create `appsettings.Development.json` in the `PlatformFlower` folder:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AWS": {
    "BucketName": "your-bucket-name",
    "Region": "ap-southeast-2",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key"
  },
  "EmailSettings": {
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password",
    "SmtpServer": "smtp.gmail.com",
    "Port": 587
  }
}
```

## Production Environment

1. Set environment variables or use AWS IAM roles:
   - `AWS__BucketName`
   - `AWS__Region`
   - `AWS__AccessKey`
   - `AWS__SecretKey`

## AWS S3 Bucket Setup

1. Create S3 bucket
2. Set bucket policy for public read access:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::your-bucket-name/*"
    }
  ]
}
```

3. Disable "Block all public access" if needed
4. Enable CORS if accessing from web:

```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["GET", "PUT", "POST", "DELETE"],
    "AllowedOrigins": ["*"],
    "ExposeHeaders": []
  }
]
```

## Security Notes

- Never commit AWS credentials to git
- Use IAM roles in production
- Rotate access keys regularly
- Use least privilege principle for IAM policies

## Required IAM Permissions

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:GetObjectMetadata"
      ],
      "Resource": "arn:aws:s3:::your-bucket-name/*"
    }
  ]
}
```
