# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0.425-bookworm-slim@sha256:ec9c0a0dc5f60adc2065762050638dd5ed5facfc53716d4580a42d86027c8e80 AS build
WORKDIR /source
COPY global.json ./
COPY src/Wukong.Core/Wukong.Core.csproj src/Wukong.Core/
COPY src/Wukong.Reports/Wukong.Reports.csproj src/Wukong.Reports/
RUN dotnet restore src/Wukong.Reports/Wukong.Reports.csproj
COPY src/Wukong.Core/ src/Wukong.Core/
COPY src/Wukong.Reports/ src/Wukong.Reports/
COPY docs/results/2026-10-08/ docs/results/2026-10-08/
COPY LICENSE ./
RUN dotnet publish src/Wukong.Reports/Wukong.Reports.csproj -c Release --no-restore \
    --self-contained false -p:UseAppHost=false -o /publish

FROM mcr.microsoft.com/dotnet/runtime:8.0.31-bookworm-slim@sha256:47c36b770db8f712ceb03e9220d799b068c769b8bc89c61d62cd41600633d9c2 AS reports
ARG SOURCE_REVISION=unknown
LABEL org.opencontainers.image.title="Wukong Reports" \
      org.opencontainers.image.description="Read saved benchmark reports and OCR JSON. Does not launch Steam or run CPU/GPU tests." \
      org.opencontainers.image.source="https://github.com/huksleva/wukong-benchmark-automation" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.revision=$SOURCE_REVISION
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app
COPY --from=build /publish/ ./
USER 1654:1654
ENTRYPOINT ["dotnet", "wukong-reports.dll"]
