﻿namespace WorkoutWotch.UnitTests.Models.Parsers
{
    using System;
    using Kent.Boogaart.PCLMock;
    using Services.Audio.Mocks;
    using Services.Delay.Mocks;
    using Services.Logger.Mocks;
    using Services.Speech.Mocks;
    using Sprache;
    using WorkoutWotch.Models.Parsers;
    using Xunit;

    public class ExerciseProgramParserFixture
    {
        [Fact]
        public void get_parser_throws_if_audio_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => ExerciseProgramParser.GetParser(null, new DelayServiceMock(), new LoggerServiceMock(), new SpeechServiceMock()));
        }

        [Fact]
        public void get_parser_throws_if_delay_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => ExerciseProgramParser.GetParser(new AudioServiceMock(), null, new LoggerServiceMock(), new SpeechServiceMock()));
        }

        [Fact]
        public void get_parser_throws_if_logger_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => ExerciseProgramParser.GetParser(new AudioServiceMock(), new DelayServiceMock(), null, new SpeechServiceMock()));
        }

        [Fact]
        public void get_parser_throws_if_speech_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => ExerciseProgramParser.GetParser(new AudioServiceMock(), new DelayServiceMock(), new LoggerServiceMock(), null));
        }

        [Theory]
        [InlineData("# foo\n", "foo")]
        [InlineData("# Foo\n", "Foo")]
        [InlineData("# Foo bar\n", "Foo bar")]
        [InlineData("# !@$%^&*()-_=+[{]};:'\",<.>/?\n", "!@$%^&*()-_=+[{]};:'\",<.>/?")]
        [InlineData("#    \t Foo   bar  \t \n", "Foo   bar")]
        public void can_parse_name(string input, string expectedName)
        {
            var result = ExerciseProgramParser
                .GetParser(
                    new AudioServiceMock(MockBehavior.Loose),
                    new DelayServiceMock(MockBehavior.Loose),
                    new LoggerServiceMock(MockBehavior.Loose),
                    new SpeechServiceMock(MockBehavior.Loose))
                .Parse(input);

            Assert.NotNull(result);
            Assert.Equal(expectedName, result.Name);
        }

        [Theory]
        [InlineData("# ignore", 0)]
        [InlineData("# ignore\n", 0)]
        [InlineData("# ignore\n## Exercise 1\n* 1 set x 1 rep", 1)]
        [InlineData("# ignore\n## Exercise 1\n* 1 set x 1 rep\n", 1)]
        [InlineData("# ignore\n\n\n\n\n\t  \t\t  \n \t \n\n \n## Exercise 1\n* 1 set x 1 rep", 1)]
        [InlineData("# ignore\n## Exercise 1\n* 1 set x 1 rep\n## Exercise 2\n* 1 set x 1 rep", 2)]
        [InlineData("# ignore\n## Exercise 1\n* 1 set x 1 rep\n  \n  \t  \n\n## Exercise 2\n* 1 set x 1 rep", 2)]
        public void can_parse_exercises(string input, int expectedExerciseCount)
        {
            var result = ExerciseProgramParser
                .GetParser(
                    new AudioServiceMock(MockBehavior.Loose),
                    new DelayServiceMock(MockBehavior.Loose),
                    new LoggerServiceMock(MockBehavior.Loose),
                    new SpeechServiceMock(MockBehavior.Loose))
                .Parse(input);

            Assert.NotNull(result);
            Assert.Equal(expectedExerciseCount, result.Exercises.Count);
        }

        [Theory]
        [InlineData("## two hashes\n")]
        [InlineData("#\n")]
        [InlineData("#no space after hash\n")]
        [InlineData("  # leading whitespace\n")]
        [InlineData("# ignore\n## Exercise 1\n* 1 set x 1 rep  ## Exercise 2\n* 1 set x 1 rep")]
        public void cannot_parse_invalid_input(string input)
        {
            var result = ExerciseProgramParser
                .GetParser(
                    new AudioServiceMock(MockBehavior.Loose),
                    new DelayServiceMock(MockBehavior.Loose),
                    new LoggerServiceMock(MockBehavior.Loose),
                    new SpeechServiceMock(MockBehavior.Loose))(new Input(input));
            Assert.False(result.WasSuccessful && result.Remainder.AtEnd);
        }
    }
}