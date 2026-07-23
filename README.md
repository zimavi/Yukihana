![logo](https://github.com/zimavi/Yukihana/raw/master/artwork/readme_logo.png)

# Yukihana OS

> An experimental operating system project built on top of the upcoming [CosmosOS Gen3](https://github.com/valentinbreiz/nativeaot-patcher).

[![License: Apache-2.0](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE.txt)
![Platform](https://img.shields.io/badge/Platform-x64%20%7C%20ARM64-purple)
![Status](https://img.shields.io/badge/Status-Experimental-orange)
![Checks](https://img.shields.io/github/check-suites/zimavi/Yukihana/master)

> [!WARNING]
> Yukihana OS is **experimental software** intended for education and research.
> It is **not** ready for production use and should **only** be run inside virtual machines such as QEMU, VMware, or VirtualBox. Running it on physical hardware is **not recommended** and may result in data loss.

---

## About

**Yukihana OS** is an experimental hobby operating system project and a complete reimagining of my previous project, **WinttOS**.

The project is built on top of the upcoming **CosmosOS Gen3**, allowing low-level architecture-specific details—such as paging, descriptor tables, interrupt handling, and scheduling to be abstracted away. This makes it easier to focus on learning higher-level operating system concepts while still designing and implementing my own kernel components.

The long-term goal is not to provide a production-ready operating system, but to create a modern and enjoyable environment for experimenting with kernel development, filesystems, security, and system architecture.

---

## Philosophy

Operating system development often requires implementing large amounts of architecture-specific code before meaningful progress can be made.

I tried multiple times, but it was quite difficult to find documentation or somewhat meaningfull tutorials, so I stick with CosmosOS. I welcome anyone to collaborate, and help me with this, as I really like learning from profecionals or just people who know more then me.

---

## Project Status

Yukihana OS is currently in an **experimental** stage.

Core infrastructure is actively being developed before moving on to userspace, graphics, and additional filesystem support.

---

## Features

### Implemented

* [x] Initramfs loading
* [x] TAR archive support
* [x] CPIO archive support
* [x] GZip decompression
* [x] BZip2 decompression
* [x] `/etc/fstab` parser
* [x] Bootloader argument parser
* [x] Structured logging framework
* [x] Configurable log formatters and sinks
* [x] Ring buffer logging
* [x] Optional resource loading groups
* [x] In-memory block device
* [x] Initial InitFS implementation

### Planned

* [ ] ext4 filesystem (WIP)
* [ ] Kernel module system
* [ ] Interactive shell
* [ ] Package manager
* [ ] Graphics stack
* [ ] Display server
* [ ] Compositor
* [ ] Window manager
* [ ] Multi-user environment
* [ ] Userspace applications

---

## Architecture

Current high-level architecture:

```text
                    Bootloader
                         │
                         ▼
                 Boot Argument Parser
                         │
                         ▼
                  Resource Loading
                         │
        ┌────────────────┴────────────────┐
        ▼                                 ▼
   Initramfs Loader                 Logger Stack
        │                                 │
        ▼                                 ▼
     Archive Layer                Formatter / Sink
        │
        ▼
      Virtual File System
```

---

## Repository Layout

```text
.
├── .github/              GitHub workflows
├── artwork/              Images used for public display
├── src/
│   └── Yukihana/
│       ├── Build/        Templates for generated files
│       ├── Boot/         Boot argument handling
│       ├── Bootloader/   Bootloader configuration
│       ├── Core/         Core utilities
│       ├── Debug/        Logging framework
│       ├── IO/           Resource loading
│       ├── Security/     Authentication
│       ├── Vfs/          Virtual filesystem (Mostly filesystems and block devices)
│       └── Resources/    Embedded resources
├── CODE_OF_CONDUCT.md
├── CONTRIBUTING.md
├── SECURITY.md
├── LICENSE.txt
├── NOTICE
└── README.md
```

---

## Building

### Requirements

* .NET 10 SDK
* CosmosOS Gen3

### Linux / macOS

```bash
git clone https://github.com/zimavi/Yukihana.git
cd Yukihana

dotnet tool install -g Cosmos.Tools
cosmos install

cosmos build -p src/Yukihana -c Release --a x64
```

Replace `Release` with `Debug`, and `x64` with `arm64` as needed. Also, for verbose building use `-v` flag.

### Windows

Install CosmosOS Gen3 using the installer provided by its releases.

Then build the project:

```powershell
git clone https://github.com/zimavi/Yukihana.git
cd Yukihana

cosmos build -p src/Yukihana -c Release --a x64
```

---

## Running

After building, the generated ISO image can be found in:

```text
output-x64/
```

or

```text
output-arm64/
```

Run the generated ISO using your preferred virtual machine.

> [!WARNING]
> Running on physical hardware is not supported.

---

## Test Disk Images

Disk images are intentionally **not** included in the repository.

Create your own GPT-formatted images yourself. Only supported format is FAT 12/16/32

Place your images in:

```text
src/Yukihana/
```

---

## Roadmap

Current development direction:

```text
✓ Core infrastructure
        │
        ▼
✓ Virtual filesystem
        │
        ▼
□ ext4 support
        │
        ▼
□ Kernel modules
        │
        ▼
□ Interactive shell
        │
        ▼
□ Package manager
        │
        ▼
□ Graphics stack
        │
        ▼
□ Multi-user environment
```

---

## Contributing

Contributions, ideas, bug reports, and discussions are always welcome.

Please read:

* [CONTRIBUTING.md](CONTRIBUTING.md)
* [SECURITY.md](SECURITY.md)

before opening issues or pull requests.

---

## License

Yukihana OS is licensed under the **Apache License 2.0**.

See [LICENSE.txt](LICENSE.txt) for details.

---

## Acknowledgements

This project would not be possible without:

* CosmosOS Gen3
* The .NET Foundation
* Everyone contributing to the open-source operating systems community
* [MrLukess](https://github.com/MrLukess) for his artwork

---

> *"Every great system starts with the courage to boot."*
