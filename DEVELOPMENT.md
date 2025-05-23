# Development

## Basics

We love pull requests. Here's a quick guide.

Fork, then clone the repo:

    git clone git@github.com:your-username/dogstatsd-csharp-client.git

### Tests

To run the tests:

```shell
dotnet test tests/StatsdClient.Tests/ --logger "console;verbosity=detailed"
```

If targeting a particular version of the framework, e.g. net6.0, then run:

```shell
dotnet test tests/StatsdClient.Tests/ --framework net6.0 --logger "console;verbosity=detailed"
```
