FROM node:22-alpine AS build
WORKDIR /src
ENV NEXT_TELEMETRY_DISABLED=1
COPY package.json package-lock.json ./
COPY apps/admin/package.json apps/admin/package.json
RUN npm ci
COPY apps/admin apps/admin
RUN npm run build:admin

FROM node:22-alpine
WORKDIR /app
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1 HOSTNAME=0.0.0.0 PORT=3000
COPY --from=build --chown=node:node /src/apps/admin/.next/standalone ./
COPY --from=build --chown=node:node /src/apps/admin/.next/static ./apps/admin/.next/static
COPY --from=build --chown=node:node /src/apps/admin/public ./apps/admin/public
USER node
EXPOSE 3000
CMD ["node", "apps/admin/server.js"]
