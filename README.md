
# bowInstaller: a tool that makes it easy to install Bash on Windows

**bowInstaller** makes it easy the installation of WSL (Windows Subsystem for Linux)

The installer will:

 * Enable developer mode (required)
 * Enable WSL feature
 * Install Bash on Windows
 * Optional: create a default user/password
 * Optional: run a post-installation script

> Note: in order to enable WSL feature you will need to reboot your computer
> during the installation

## GUI Wizard

> Work in progress: currently not available

You can install Bash on Windows using the wizard:

```
bowinstaller.msi
```

<!--
# TODO: add a screenshot
-->

## CLI for automated installations

A command-line interface is also available, specially useful if you need to install Bash on Windows in an automated way:

### Usage

```
bowinstaller.exe <options>

Options:
  -u, --user <username>        Default user
  -p, --password <password>    Default user's password
  -y, --assumeyes              Assume yes (the computer will be restarted!)
  -r, --resume                 Resume installation after the reboot
  -p, --postinstall            Post-installation script
      --version                Print version information
  -v, --verbose                Print debug information
  -h, --help                   Print this help
```

If no user is specified, `root` will be the default user (with no password).

### Examples

```bat
# Interactive installation (cannot specify default user):
bowinstaller.exe

# Automated installation:
bowinstaller.exe --user myuser --password mypassword --postinstall C:\myscript.bat
```
