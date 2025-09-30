# ---------------------------------------
# Stage 1: build (Vite)
# ---------------------------------------
FROM node:22-alpine AS build
ARG BUILD_MODE=production
ARG VITE_API_BASE_URL=http://localhost:8080

WORKDIR /src

# Reproducible installs in CI/containers
ENV NPM_CONFIG_FUND=false \
    NPM_CONFIG_AUDIT=false

# Copy manifests first to leverage layer caching
COPY package.json package-lock.json* ./
RUN npm ci

# Copy full source
COPY . .

# Build static assets
ENV NODE_ENV=${BUILD_MODE} \
    VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

# ---------------------------------------
# Stage 2: runtime (Nginx)
# ---------------------------------------
FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

# OCI labels
LABEL org.opencontainers.image.title="CollabTask Web" \
      org.opencontainers.image.description="React (Vite) frontend for CollabTask" \
      org.opencontainers.image.source="https://github.com/borjaoviedo/collab-task" \
      org.opencontainers.image.licenses="MIT"

# Minimal tooling for healthcheck
RUN apk add --no-cache curl

# Nginx config with SPA fallback
RUN printf "include /etc/nginx/mime.types;\n\
server {\n\
  listen 8080;\n\
  server_name _;\n\
  root /usr/share/nginx/html;\n\
  index index.html;\n\
  location / {\n\
    try_files \$uri /index.html;\n\
  }\n\
}\n" > /etc/nginx/conf.d/default.conf

# Copy built app
COPY --from=build /src/dist /usr/share/nginx/html

# Network
EXPOSE 8080

# Container-level healthcheck
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD curl -fsS http://localhost:8080/ || exit 1

# Entrypoint
CMD ["nginx","-g","daemon off;"]
