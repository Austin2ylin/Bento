FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
ARG VITE_BENTO_GATEWAY_BASE_URL=http://localhost:5000/gateway
ARG VITE_BENTO_CACHE_ADMIN_KEY=
ENV VITE_BENTO_GATEWAY_BASE_URL=${VITE_BENTO_GATEWAY_BASE_URL}
ENV VITE_BENTO_CACHE_ADMIN_KEY=${VITE_BENTO_CACHE_ADMIN_KEY}
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
