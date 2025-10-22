FROM mcr.microsoft.com/dotnet/sdk:9.0.306-bookworm-slim@sha256:ca77338a19f87a7d24494a3656cb7d878a040c158621337b9cd3ab811c5eb057

RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y \
        dotnet-sdk-8.0 \
        cmake \
        clang \
        make

WORKDIR /project
