FROM nginx:1.28-alpine
COPY apps/client/nginx.conf /etc/nginx/conf.d/default.conf
COPY apps/client/build/web /usr/share/nginx/html
EXPOSE 80
