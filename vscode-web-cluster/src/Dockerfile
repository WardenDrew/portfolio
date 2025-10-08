FROM ubuntu:24.10

ARG CODE_PORT=8080

# Update Apt Repos
RUN apt update

# Setup General Dependencies
RUN apt install -y \
    git \
    libatomic1 \
    nano \
    net-tools \
    sudo \
    curl \
    wget

# Setup C# / Dotnet 9
RUN apt install -y dotnet-sdk-9.0

# Install Code-Server
RUN mkdir -p /app/code-server
RUN curl -o \
    /tmp/code-server.tar.gz -L \
    "https://github.com/coder/code-server/releases/download/v4.99.2/code-server-4.99.2-linux-amd64.tar.gz"
RUN tar xf \
    /tmp/code-server.tar.gz -C \
    /app/code-server --strip-components=1

# Install extensions
RUN mkdir -p /app/extensions
RUN /app/code-server/bin/code-server --extensions-dir /app/extensions --install-extension ms-dotnettools.vscode-dotnet-runtime
RUN /app/code-server/bin/code-server --extensions-dir /app/extensions --install-extension muhammad-sammy.csharp

# Cleanup Setup Steps
RUN apt clean
RUN rm -rf \
    /tmp/* \
    /var/lib/apt/lists/* \
    /var/tmp/*

# Copy Entrypoint script
COPY startup /startup
RUN chmod +x /startup/entrypoint.sh
RUN chmod +x /startup/userentrypoint.sh
RUN chmod -R 775 /startup

# Copy Defaults
COPY default /default
RUN chmod -R 775 /default

# Setup initial folders
RUN mkdir -p /config
RUN mkdir -p /projects

EXPOSE 8080
ENTRYPOINT ["/startup/entrypoint.sh"]