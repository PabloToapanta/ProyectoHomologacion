FROM node:24-bookworm-slim AS build
WORKDIR /source
COPY package.json package-lock.json ./
RUN npm ci --no-fund
COPY . .
RUN npm run build

FROM nginx:stable-alpine AS runtime
ENV VITE_GATEWAY_URL=http://gateway:8080
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /source/dist/ /usr/share/nginx/html/
EXPOSE 8080
