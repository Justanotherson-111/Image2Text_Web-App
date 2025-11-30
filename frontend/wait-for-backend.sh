#!/bin/sh

echo "⏳ Waiting for backend to resolve..."

until ping -c 1 backend >/dev/null 2>&1; do
  echo "Backend not ready, retrying..."
  sleep 2
done

echo "✅ Backend detected. Starting nginx..."
nginx -g 'daemon off;'
