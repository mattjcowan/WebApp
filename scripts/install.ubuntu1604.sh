#!/bin/bash
# dir=$(pwd)
# cdir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
# cd $cdir

# -------------------------
# INSTRUCTIONS
# -------------------------
# This script will install the WebApp onto a barebones ubuntu 16.04 machine.
#
# Run using:
# cd ~/ && curl -s https://raw.githubusercontent.com/mattjcowan/WebApp/develop/scripts/install.ubuntu1604.sh -o install.webapp.sh && chmod +x install.webapp.sh && ./install.webapp.sh && rm -f install.webapp.sh
# OR
# repo=mattjcowan/WebApp && mkdir -p /home/apps/$repo && cd /home/apps/$repo && git clone -b master https://github.com/$repo . && ./dist/install.ubuntu1604.sh

APP_NAME=WebApp
DEPLOY_DIR=/var/www/webapp
BIN_DIR=$DEPLOY_DIR/bin
WEB_DIR=$DEPLOY_DIR/web
DATA_DIR=$DEPLOY_DIR/data
PLUGINS_DIR=$DEPLOY_DIR/plugins

GITHUB_REPO_NAME=mattjcowan/WebApp
GITHUB_REPO_URL=https://github.com/$GITHUB_REPO_NAME
SCRIPTS_DIR=/home/apps
LOCAL_DIR=$SCRIPTS_DIR/$GITHUB_REPO_NAME

CDN_HOSTS="https://ssl.google-analytics.com https://fonts.googleapis.com https://cdn.jsdelivr.net https://maxcdn.bootstrapcdn.com https://code.jquery.com https://cdnjs.cloudflare.com https://stackpath.bootstrapcdn.com"

mkdir -p $DEPLOY_DIR
mkdir -p $BIN_DIR
mkdir -p $WEB_DIR
mkdir -p $DATA_DIR
mkdir -p $PLUGINS_DIR
mkdir -p $SCRIPTS_DIR

cd $SCRIPTS_DIR

# -------------------------
# Install Unattended Upgrades
# -------------------------

sudo apt-get update
# sudo apt-get dist-upgrade
# sudo apt-get autoremove
sudo apt-get install unattended-upgrades -y

if [ ! -f /etc/apt/apt.conf.d/20auto-upgrades ]; then
cat >/etc/apt/apt.conf.d/20auto-upgrades <<EOL
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Download-Upgradeable-Packages "1";
APT::Periodic::AutocleanInterval "3";
APT::Periodic::Unattended-Upgrade "1";
EOL
fi

# -------------------------
# Get Public IP
# -------------------------

sudo apt-get install curl -y
publicip=$(curl -4 icanhazip.com)

# -------------------------
# Install Environment Variables
# -------------------------

grep -qF "DOTNET_CLI_HOME" "/etc/environment"  || echo "DOTNET_CLI_HOME=\"/tmp\"" | sudo tee --append "/etc/environment"
source /etc/environment
export `cat /etc/environment`

# -------------------------
# Install Libraries
# -------------------------

sudo apt-get update
sudo apt-get install nano -y
sudo apt-get install git -y
sudo apt-get install sqlite3 -y
sudo apt-get install libsqlite3-dev -y
sudo apt-get install ufw -y
sudo apt-get install python-pip -y

# -------------------------
# Install Node.JS
# -------------------------

curl -sL https://deb.nodesource.com/setup_10.x | sudo bash -
sudo apt-get update
sudo apt-get install nodejs -y
npm install -g yarn
npm install -g npx
npm install -g np
npm install -g npm-name-cli
npm install -g tldr
npm install -g now
npm install -g gulp
npm install -g less
npm install -g node-sass
npm install -g rimraf
npm install -g dotenv
npm install -g pm2
npm install -g forever
npm install -g nodemon
npm install -g http-server

# -------------------------
# Install .net core 2.2
# -------------------------

wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get install apt-transport-https -y
sudo apt-get update
sudo apt-get install dotnet-sdk-2.2 -y

# -------------------------
# Install NGINX
# -------------------------

sudo apt-get update
sudo apt-get install nginx -y
sudo systemctl enable nginx
sudo systemctl restart nginx

# create self-signed cert (valid for more than 5 years, why not), because we are running
# dhparam, this will take a while!!
publicip="$(dig +short myip.opendns.com @resolver1.opendns.com)"

[ -f /etc/ssl/private/nginx-selfsigned.key ] || sudo openssl req -x509 -nodes -days 2000 -newkey rsa:4096 -keyout /etc/ssl/private/nginx-selfsigned.key -out /etc/ssl/certs/nginx-selfsigned.crt -subj /C=US/ST=Illinois/L=Chicago/O=Startup/CN=$publicip
[ -f /etc/ssl/certs/dhparam.pem ] || sudo openssl dhparam -dsaparam -out /etc/ssl/certs/dhparam.pem 4096 > /dev/null 2>&1

if [ ! -f /etc/nginx/snippets/self-signed.conf ]; then
cat >/etc/nginx/snippets/self-signed.conf <<EOL
ssl_certificate /etc/ssl/certs/nginx-selfsigned.crt;
ssl_certificate_key /etc/ssl/private/nginx-selfsigned.key;
EOL
fi

if [ ! -f /etc/nginx/snippets/ssl-params.conf ]; then
cat >/etc/nginx/snippets/ssl-params.conf <<EOL
# from https://cipherli.st/ and https://raymii.org/s/tutorials/Strong_SSL_Security_On_nginx.html
ssl_protocols TLSv1.2;
ssl_prefer_server_ciphers on;
ssl_ciphers "EECDH+AESGCM:EDH+AESGCM:AES256+EECDH:AES256+EDH";
ssl_ecdh_curve secp384r1;
ssl_session_cache shared:SSL:10m;
ssl_session_tickets off;
ssl_stapling on;
ssl_stapling_verify on;
resolver 8.8.8.8 8.8.4.4 valid=300s;
resolver_timeout 5s;
add_header Strict-Transport-Security "max-age=63072000; includeSubdomains";
add_header X-Frame-Options DENY;
add_header X-Content-Type-Options nosniff;
ssl_dhparam /etc/ssl/certs/dhparam.pem;
EOL
fi

# the following will direct all traffic on the VM to this site
if [ ! -f /etc/nginx/sites-available/default ]; then
cat >/etc/nginx/sites-available/default <<EOL
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    listen 443 ssl http2 default_server;
    listen [::]:443 ssl http2 default_server;
    access_log /var/log/nginx/webapp.access.log;
    error_log /var/log/nginx/webapp.error.log;
    server_tokens off;
    server_name $publicip;
    include snippets/self-signed.conf;
    include snippets/ssl-params.conf;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;

        client_max_body_size 500m;
        client_body_buffer_size 128k;

        proxy_connect_timeout  90;
        proxy_send_timeout 90;
        proxy_read_timeout 90;
        # proxy_buffers 32 4k;
        proxy_buffering off;
        proxy_ignore_client_abort off;
        proxy_intercept_errors on;
        proxy_pass_request_headers on;

        proxy_hide_header X-Content-Type-Options;

        # The following are needed for a perfect security score
        # get a grade A in security at https://securityheaders.io
        add_header X-Frame-Options SAMEORIGIN;
        add_header X-Content-Type-Options nosniff;
        #add_header X-Content-Type-Options "" always;
        add_header X-XSS-Protection "1; mode=block";
        add_header Strict-Transport-Security "max-age=31536000; includeSubdomains; preload";
        add_header Content-Security-Policy "default-src https: 'self' $CDN_HOSTS; script-src https: 'self' 'unsafe-inline' 'unsafe-eval' $CDN_HOSTS; img-src https: 'self' $CDN_HOSTS; style-src 'self' 'unsafe-inline' $CDN_HOSTS; font-src https: 'self' $CDN_HOSTS; frame-src $CDN_HOSTS; object-src 'none'";
        add_header Referrer-Policy "no-referrer";
    }
}
EOL
fi

# -------------------------
# Install UFW
# -------------------------

sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow ssh
sudo ufw allow http
sudo ufw allow https
sudo ufw --force enable

# -------------------------
# Install WebApp
# -------------------------

if [ ! -d $LOCAL_DIR ]; then
    mkdir -p $LOCAL_DIR
    cd $LOCAL_DIR
    git clone -b master $GITHUB_REPO_URL .
else
    cd $LOCAL_DIR
    git checkout master
    git pull
fi

cd $LOCAL_DIR
npm install
cd $LOCAL_DIR/src/WebApp
dotnet publish -c release -o $SCRIPTS_DIR/$GITHUB_REPO_NAME/dist
cp -rv $SCRIPTS_DIR/$GITHUB_REPO_NAME/dist/* $BIN_DIR
cp -rv $LOCAL_DIR/node_modules/* $BIN_DIR/node_modules
sudo chown -R www-data:www-data $DEPLOY_DIR/
sudo chmod -R 755 $DEPLOY_DIR/

# create system.d service
if [ ! -f /etc/systemd/system/webapp.service ]; then
cat >/etc/systemd/system/webapp.service <<EOL
[Unit]
Description=WebApp Service
[Service]
WorkingDirectory=$BIN_DIR
ExecStart=/usr/bin/dotnet $BIN_DIR/WebApp.dll
Restart=always
RestartSec=10
SyslogIdentifier=dotnet-webapp
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=DATA_DIR=$DATA_DIR
Environment=BIN_DIR=$BIN_DIR
Environment=WEB_DIR=$WEB_DIR
Environment=PLUGINS_DIR=$PLUGINS_DIR
[Install]
WantedBy=multi-user.target
EOL
fi

cat >$SCRIPTS_DIR/webapp.refresh.sh <<EOL
cd $LOCAL_DIR
changed=0
git remote update && git status -uno | grep -q 'Your branch is behind' && changed=1
if [ \$changed = 1 ]; then
    git pull
    npm install
    cd src/WebApp
    dotnet publish -c release -o $SCRIPTS_DIR/$GITHUB_REPO_NAME/dist
    cp -rv $SCRIPTS_DIR/$GITHUB_REPO_NAME/dist/* $BIN_DIR
    cp -rv $LOCAL_DIR/node_modules/* $BIN_DIR/node_modules
    sudo chown -R www-data:www-data $DEPLOY_DIR/
    sudo chmod -R 755 $DEPLOY_DIR/
    sudo systemctl try-restart webapp.service
    echo "Updated successfully";
else
    echo "Up to date"
fi
EOL

chmod +x $SCRIPTS_DIR/webapp.refresh.sh

cat >/etc/cron.d/refresh_webapp_five_minute <<EOL
5 * * * * root /bin/sh $SCRIPTS_DIR/webapp.refresh.sh > $SCRIPTS_DIR/webapp.refresh.log 2>&1
EOL

sudo systemctl enable webapp.service
sudo systemctl start webapp.service

# -------------------------
# Restart Services and Reboot
# -------------------------

sudo service webapp restart
sudo service nginx restart
# sudo reboot
