#!/bin/sh
# Creates the dedicated, public-read product-image bucket. Only product images may ever be stored in it.
set -e
BUCKET=commercehub-product-images
awslocal s3api create-bucket --bucket "$BUCKET" 2>/dev/null || true
awslocal s3api put-bucket-policy --bucket "$BUCKET" --policy "{
  \"Version\": \"2012-10-17\",
  \"Statement\": [{ \"Effect\": \"Allow\", \"Principal\": \"*\", \"Action\": \"s3:GetObject\", \"Resource\": \"arn:aws:s3:::$BUCKET/*\" }]
}"
awslocal s3api put-bucket-cors --bucket "$BUCKET" --cors-configuration '{
  "CORSRules": [{ "AllowedOrigins": ["*"], "AllowedMethods": ["PUT", "GET"], "AllowedHeaders": ["*"] }]
}'
