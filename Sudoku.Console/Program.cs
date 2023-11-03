using Spectre.Console.Cli;
using Sudoku.Console.Commands;

var app = new CommandApp<GenerateBoard>();
app.Configure(config =>
{
#if DEBUG
	config.PropagateExceptions();
	config.ValidateExamples();
#endif
});

app.Run(args);