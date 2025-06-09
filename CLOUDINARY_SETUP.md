# Cloudinary Configuration Setup

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
  "Cloudinary": {
    "CloudName": "your-cloud-name",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret"
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

1. Set environment variables:
   - `Cloudinary__CloudName`
   - `Cloudinary__ApiKey`
   - `Cloudinary__ApiSecret`

## Cloudinary Account Setup

1. **Sign up for Cloudinary**: https://cloudinary.com/users/register/free
2. **Get your credentials** from Dashboard:
   - Cloud Name
   - API Key
   - API Secret

## Cloudinary Features Used

- **Auto optimization**: Quality and format optimization
- **Secure URLs**: HTTPS by default
- **Folder organization**: Files organized by folders
- **Unique public IDs**: Prevent filename conflicts
- **Auto deletion**: Remove old avatars when uploading new ones

## Security Notes

- Never commit Cloudinary credentials to git
- Use environment variables in production
- Rotate API keys regularly
- Use signed uploads for sensitive content

## Cloudinary Advantages over AWS S3

✅ **Built for images/videos**: Automatic optimization and transformations  
✅ **Easier setup**: No complex bucket policies or ACL configurations  
✅ **Auto CDN**: Global CDN included by default  
✅ **Image transformations**: Resize, crop, format conversion on-the-fly  
✅ **Better pricing**: Generous free tier for small projects  
✅ **Simpler API**: More intuitive for media management  

## Upload Limits

- **Free tier**: 25 GB storage, 25 GB bandwidth/month
- **File size**: Up to 100MB per file (we limit to 5MB)
- **Formats**: All major image formats supported

## Example URLs

```
Original: https://res.cloudinary.com/your-cloud/image/upload/v1234567890/avatars/uuid.jpg
Optimized: https://res.cloudinary.com/your-cloud/image/upload/q_auto,f_auto/avatars/uuid.jpg
Resized: https://res.cloudinary.com/your-cloud/image/upload/w_300,h_300,c_fill/avatars/uuid.jpg
```
