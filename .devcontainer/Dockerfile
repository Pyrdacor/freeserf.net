FROM --platform=linux/amd64 conneqthub/dotnet-maui-android:latest

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends libglfw3 sudo \
    && rm -rf /var/lib/apt/lists

RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet --no-path \
    && rm /tmp/dotnet-install.sh

RUN echo "ubuntu ALL=(root) NOPASSWD:ALL" > /etc/sudoers.d/ubuntu \
    && chmod 0440 /etc/sudoers.d/ubuntu
