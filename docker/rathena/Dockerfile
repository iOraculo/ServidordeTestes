# ==============================================================================
# AureumRO — Servidor de Testes (rAthena Pré-Renewal 13.3)
# Multi-stage build: compila rAthena em modo Pré-Renewal com PACKETVER 20220406
# ==============================================================================

FROM debian:bookworm-slim AS builder

ARG RATHENA_GIT_REF=master
ARG RATHENA_PACKETVER=20220406
ARG RATHENA_DISABLE_OBFUSCATION=1

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    git \
    build-essential \
    autoconf \
    automake \
    pkg-config \
    libmariadb-dev \
    libmariadb-dev-compat \
    libpcre3-dev \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /rathena

RUN git clone --depth 1 --branch "${RATHENA_GIT_REF}" https://github.com/rathena/rathena.git /rathena

# Permite desativar packet obfuscation para clientes Ragexe de teste sem criptografia
RUN if [ "${RATHENA_DISABLE_OBFUSCATION}" = "1" ]; then \
      printf '\n// AureumRO Test Server: desativa ofuscação para Ragexe de teste\n#undef PACKET_OBFUSCATION\n' >> src/custom/defines_post.hpp; \
    fi

# Compila login-server, char-server e map-server em modo Pré-Renewal com VIP
RUN ./configure \
      --enable-prere=yes \
      --enable-packetver="${RATHENA_PACKETVER}" \
      --enable-vip=yes \
    && make -j"$(nproc)" server \
    && rm -rf .git src/obj* 3rdparty/*/obj*

# ------------------------------------------------------------------------------
# Imagem final de execução (runtime enxuto)
# ------------------------------------------------------------------------------
FROM debian:bookworm-slim AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libmariadb3 \
    libpcre3 \
    zlib1g \
    mariadb-client \
    gettext-base \
    netcat-openbsd \
    procps \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /rathena

COPY --from=builder /rathena /rathena
COPY docker/rathena/entrypoint.sh /usr/local/bin/rathena-entrypoint.sh
COPY docker/rathena/healthcheck.sh /usr/local/bin/rathena-healthcheck.sh

RUN chmod +x /usr/local/bin/rathena-entrypoint.sh /usr/local/bin/rathena-healthcheck.sh \
    && mkdir -p /rathena/log /rathena/conf/import /rathena/db/import /aureumro/state

EXPOSE 6900 6121 5121

ENTRYPOINT ["/usr/local/bin/rathena-entrypoint.sh"]
