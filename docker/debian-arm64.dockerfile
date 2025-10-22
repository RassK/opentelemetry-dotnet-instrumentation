FROM mcr.microsoft.com/dotnet/sdk:9.0.306-bookworm-slim@sha256:ca77338a19f87a7d24494a3656cb7d878a040c158621337b9cd3ab811c5eb057

RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

# Install older SDKs using the install script as there are no arm64 SDK packages.
RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 8.0.415 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
