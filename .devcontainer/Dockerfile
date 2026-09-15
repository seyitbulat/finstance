FROM node:24-bookworm
RUN apt-get update && apt-get install -y --no-install-recommends git curl ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /workspace/ui/finstance/node_modules /workspace/ui/finstance/.next \
    && chown -R node:node /workspace
USER node
WORKDIR /workspace/ui/finstance
ENV NEXT_TELEMETRY_DISABLED=1
CMD ["sleep", "infinity"]
