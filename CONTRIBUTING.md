# Contributing to PedX Proxy

We appreciate your interest in contributing to the PedX Proxy project. This document provides guidelines for contributions to help maintain quality and consistency.

## Licensing

As noted in [LICENSE.md](LICENSE.md), this project is licensed under the MIT License. By submitting a pull request, you are agreeing that your contributions will be licensed under the same terms.

## How to Contribute

- Report bugs and request features via [Github Issues](https://github.com/).
- Contact Victoria Coulter (vcoulter@dot.ga.gov) with ideas on how to improve the PedX Proxy.
- Create a [Github pull request](https://help.github.com/articles/creating-a-pull-request/) with new functionality or fixing a bug.
- Help improve documentation and best practices.

## Development Guidelines

### Architecture

The PedX Proxy is designed with an extensible architecture to support multiple traffic controller systems. When adding support for a new controller type:

1. Implement the `IAdapter` interface
2. Register the new adapter in the `AdapterFactory`
3. Update the configuration system to support the new controller type

### Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Include XML documentation for public APIs
- Write unit tests for new functionality
- Ensure all existing tests pass before submitting a pull request

### Pull Request Process

1. Fork the repository and create a feature branch from `main`
2. Make your changes with clear commit messages
3. Add or update tests as necessary
4. Update documentation including comments and README if needed
5. Submit a pull request with a description of your changes
6. Respond to any feedback during the review process

For each pull request, a project maintainer will be assigned to review the changes. For minor fixes, the pull request may be merged right away. For larger changes, we may have comments or request clarifications.

## Issue Guidelines

When submitting issues, please follow these guidelines:

- **Specific**: Make specific requests (e.g., "Add support for XYZ controller")
- **Measurable**: Include clear requirements so we know when the issue is resolved
- **Actionable**: Request specific actions that can be taken
- **Realistic**: Ensure the issue is achievable within the project's scope
- **Time-Aware**: Consider breaking larger issues into smaller, manageable parts

## Releasing Changes

Contributions will be released in accordance with GitHub policies and under the MIT License applied to this repository.

## Questions?

If you have questions about contributing that aren't answered here, please contact Victoria Coulter at vcoulter@dot.ga.gov.

Thank you for contributing to making pedestrian crossings safer and more accessible!
