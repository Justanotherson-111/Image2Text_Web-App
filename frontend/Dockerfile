# ================= Build React App =================
FROM node:20-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build


# ================= Nginx =================
FROM nginx:1.25-alpine
WORKDIR /usr/share/nginx/html

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy your nginx config & certs
COPY default.conf /etc/nginx/conf.d/default.conf
COPY certs /etc/nginx/certs

# Ensure filesystem is writable (fix read-only issue)
RUN chmod -R 755 /etc/nginx \
    && chmod -R 755 /etc/nginx/conf.d \
    && chmod -R 755 /usr/share/nginx/html

# Install tools for waiting backend
RUN apk add --no-cache bash curl

# Copy React build
COPY --from=builder /app/dist .

# Add wait script to delay nginx startup until backend is reachable
COPY wait-for-backend.sh /wait-for-backend.sh
RUN chmod +x /wait-for-backend.sh

EXPOSE 80 443

CMD ["/wait-for-backend.sh"]
