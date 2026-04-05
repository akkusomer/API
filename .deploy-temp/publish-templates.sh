#!/usr/bin/env bash
set -euo pipefail
install -d -m 0755 /var/www/atlasweb-templates
rm -rf /var/www/atlasweb-templates/*
tar -xzf /home/akkusomer0742/templates.tgz -C /var/www/atlasweb-templates
chown -R www-data:www-data /var/www/atlasweb-templates
cat > /etc/nginx/sites-available/atlasweb-api <<'EOF'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;

    location = /templates {
        return 302 /templates/login.html;
    }

    location = /templates/ {
        return 302 /templates/login.html;
    }

    location /templates/ {
        alias /var/www/atlasweb-templates/;
        autoindex off;
        try_files $uri $uri/ =404;
    }

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_cache_bypass $http_upgrade;
    }
}
EOF
nginx -t
systemctl restart nginx
printf 'LOCAL_TEMPLATE_HEADERS\n'
curl -I --max-time 10 http://127.0.0.1/templates/login.html || true
printf 'LOCAL_SWAGGER_HEADERS\n'
curl -I --max-time 10 http://127.0.0.1/ || true
