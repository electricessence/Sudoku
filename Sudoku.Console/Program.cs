using Spectre.Console.Cli;
using Sudoku.Console.Commands;

var app = new CommandApp<SubsetCombinations>();
app.Configure(config =>
{
#if DEBUG
	config.PropagateExceptions();
	config.ValidateExamples();
#endif
});

app.Run(args);