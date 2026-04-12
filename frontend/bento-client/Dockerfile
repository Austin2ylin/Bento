FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
ARG VITE_BENTO_GATEWAY_BASE_URL=http://localhost:5000/gateway
ENV VITE_BENTO_GATEWAY_BASE_URL=${VITE_BENTO_GATEWAY_BASE_URL}
RUN npm run build

# nginx-unprivileged：以非 root 使用者（uid 101）執行，避免 root 提權風險
# 內部監聽 8080（非特權 port），docker-compose 對外映射 3000:8080
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
