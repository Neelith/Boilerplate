# Boilerplate

## Overview

This application generates files based on templates and user input. 
It is designed to be flexible and easy to use, allowing users to customize the output according to their needs.

## Usage

To run the application, use the following command:
## Options

| Option                | Description                                         |
|-----------------------|-----------------------------------------------------|
| `--help` or `-h`      | Show help information                               |
| `--group` or `-g`     | Template group (subfolder under templates)          |
| `--output` or `-o`    | Base output directory for generated files           |
| `--prefix` or `-p`    | Prefix for generated file names                     |
| `--suffix` or `-s`    | Suffix for generated file names                     |
| `--vars` or `-vs`     | Comma-separated key=value pairs for template variables |

## Example
boilerplate -g your-group-name -fn GetMovements -o .\YourProjectName.Application\Features\GetMovements -vs QueryName=GetMovements

## License

This project is licensed under the MIT License.
