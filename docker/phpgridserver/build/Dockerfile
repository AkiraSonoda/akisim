#
# NOTE: THIS DOCKERFILE IS GENERATED VIA "apply-templates.sh"
#
# PLEASE DO NOT EDIT IT DIRECTLY.
#

FROM debian:bullseye


# persistent / runtime deps
RUN set -eux; \
	apt-get update; \
	apt-get install -y \
		ca-certificates \
		curl \
		xz-utils \
		php \
		php-imagick \
		php7.4-gmp \
		php7.4-curl \
		php7.4-gd \
        php7.4-mysql \
		php-xdebug \
 	; \
	rm -rf /var/lib/apt/lists/*
	
ENV APACHE_CONFDIR /etc/apache2
ENV APACHE_ENVVARS $APACHE_CONFDIR/envvars

RUN rm -f /etc/php/7.4/apache2/conf.d/20-xdebug.ini

COPY apache2.conf /etc/apache2
COPY php.ini /etc/php/7.4/apache2
COPY 90-xdebug.ini /etc/php/7.4/apache2/conf.d

# RUN set -eux; \

## delete the "index.html" that installing Apache drops in here
#	rm -rvf /var/www/html/*; \
##	\
## logs should go to stdout / stderr
#	ln -sfT /dev/stderr "$APACHE_LOG_DIR/error.log"; \
#	ln -sfT /dev/stdout "$APACHE_LOG_DIR/access.log"; \
#	ln -sfT /dev/stdout "$APACHE_LOG_DIR/other_vhosts_access.log"; \
#	chown -R --no-dereference "$APACHE_RUN_USER:$APACHE_RUN_GROUP" "$APACHE_LOG_DIR"
#
## Apache + PHP requires preforking Apache for best results
# RUN a2dismod mpm_event && a2enmod mpm_prefork && a2dismod reqtimeout && a2enmod rewrite
# AKIDO Did not enable mpm_prefork
RUN a2dismod mpm_event && a2dismod reqtimeout && a2enmod rewrite

COPY phpgridserver.conf /etc/apache2/conf-available
RUN a2enconf phpgridserver

COPY 000-default.conf /etc/apache2/sites-available
RUN a2ensite 000-default

COPY docker-php-ext-* docker-php-entrypoint /usr/local/bin/

ENTRYPOINT ["docker-php-entrypoint"]
# https://httpd.apache.org/docs/2.4/stopping.html#gracefulstop
# STOPSIGNAL SIGWINCH

COPY apache2-foreground /usr/local/bin/
WORKDIR /var/www

EXPOSE 80
CMD ["apache2-foreground"]
