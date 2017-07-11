using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Pong
{
    struct Ball
    {
        public Texture2D Texture;
        public Vector2 Position;
        public Vector2 Direction;
        public float Speed;

        public Rectangle CollisionRect {
            get { return new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height); }
        }
    }


    struct Paddle
    {
        public Texture2D Texture;
        public Vector2 Position;
        public int Score;

        public Keys MoveUpKey;
        public Keys MoveDownKey;

        public Rectangle CollisionRect {
            get { return new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height); }
        }
    }


    class PongGame : Microsoft.Xna.Framework.Game
    {
        const int WINDOW_WIDTH = 800;
        const int WINDOW_HEIGHT = 600;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D backgroundTexture;
        SpriteFont scoreFont;
        SoundEffect hitSound;

        Ball ball;
        Paddle leftPaddle;
        Paddle rightPaddle;

        Random rand = new Random();
        bool gameOver;


        public PongGame()
        {
            graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = WINDOW_WIDTH,
                PreferredBackBufferHeight = WINDOW_HEIGHT,
                IsFullScreen = false
            };

            Content.RootDirectory = "Content";
        }


        protected override void LoadContent()
        {
            // Create the spritebatch.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load the textures, fonts and sounds.
            backgroundTexture = Content.Load<Texture2D>("textures/background");
            ball.Texture = Content.Load<Texture2D>("textures/ball");
            leftPaddle.Texture = rightPaddle.Texture = Content.Load<Texture2D>("textures/paddle");

            scoreFont = Content.Load<SpriteFont>("fonts/scorefont");
            hitSound = Content.Load<SoundEffect>("sounds/hit");

            // Set the initial paddle positions.
            leftPaddle.Position = new Vector2(20, (WINDOW_HEIGHT - leftPaddle.Texture.Height) / 2f);
            rightPaddle.Position = new Vector2(
                (WINDOW_WIDTH - rightPaddle.Texture.Width) - 20,
                (WINDOW_HEIGHT - rightPaddle.Texture.Height) / 2f);

            // Set paddle movement keys.
            leftPaddle.MoveUpKey = Keys.W;
            leftPaddle.MoveDownKey = Keys.S;
            rightPaddle.MoveUpKey = Keys.Up;
            rightPaddle.MoveDownKey = Keys.Down;

            // Set the ball to its initial state.
            ResetBall();
        }


        protected override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (!gameOver)
            {
                CheckPaddleInput(delta, ref leftPaddle);
                CheckPaddleInput(delta, ref rightPaddle);

                UpdateBall(delta);
            }

            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            if (!gameOver)
            {
                // Draw the background.
                spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);

                // Draw the ball and paddles.
                spriteBatch.Draw(ball.Texture, ball.Position, Color.White);
                spriteBatch.Draw(leftPaddle.Texture, leftPaddle.Position, Color.White);
                spriteBatch.Draw(rightPaddle.Texture, rightPaddle.Position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally, 1f);

                // Draw the scores.
                spriteBatch.DrawString(scoreFont, "Score: " + leftPaddle.Score, new Vector2(10, 5), Color.White);
                spriteBatch.DrawString(scoreFont, "Score: " + rightPaddle.Score, new Vector2(WINDOW_WIDTH - 150, 5), Color.White);
            }
            else
            {
                // If the game is over, display the scores.

                string winner = (leftPaddle.Score >= 5 ? "left player" : "right player");
                int winningScore = (leftPaddle.Score >= 5 ? leftPaddle.Score : rightPaddle.Score);
                int losingScore = (leftPaddle.Score >= 5 ? rightPaddle.Score : leftPaddle.Score); ;

                DrawCenteredText(spriteBatch, scoreFont, string.Format("Congratulations, {0}! You won!", winner));
                DrawCenteredText(spriteBatch, scoreFont, string.Format("Your score was {0} and your opponent's score was {1}.", winningScore, losingScore), new Vector2(0, 40));
            }
            
            spriteBatch.End();

            base.Draw(gameTime);
        }


        private void DrawCenteredText(SpriteBatch batch, SpriteFont font, string text, Vector2 offset=default(Vector2))
        {
            var textPosition = new Vector2(
                    (WINDOW_WIDTH - font.MeasureString(text).X) / 2,
                    (WINDOW_HEIGHT - font.MeasureString(text).Y) / 2);

            batch.DrawString(font, text, textPosition + offset, Color.White);
        }


        private void CheckPaddleInput(float delta, ref Paddle paddle)
        {
            KeyboardState keyboard = Keyboard.GetState();

            // Padding at the top and bottom of the screen.
            int padding = 50;

            // Check if the player wants to move upward.
            if (keyboard.IsKeyDown(paddle.MoveUpKey) && paddle.Position.Y > padding)
                paddle.Position.Y -= 300 * delta;

            // Check if the player wants to move downward.
            if (keyboard.IsKeyDown(paddle.MoveDownKey) && paddle.Position.Y < (WINDOW_HEIGHT - padding) - paddle.Texture.Height)
                paddle.Position.Y += 300 * delta;
        }


        private void UpdateBall(float delta)
        {
            // Update the ball's position based on its direction and speed.
            ball.Position += (ball.Direction * ball.Speed) * delta;

            // Slowly increase the ball's speed over time.
            ball.Speed += 20 * delta;

            // Check if the ball leaves the screen on the left.
            if (ball.Position.X + ball.Texture.Width <= 0)
            {
                rightPaddle.Score++;
                ResetBall();
            }

            // Check if the ball leaves the screen on the right.
            if (ball.Position.X >= WINDOW_WIDTH)
            {
                leftPaddle.Score++;
                ResetBall();
            }

            // Check if the ball hits the top or bottom of the screen.
            const int padding = 50;
            if (ball.Position.Y <= padding || ball.Position.Y + ball.Texture.Height >= WINDOW_HEIGHT - padding)
            {
                ball.Direction.Y *= -1;
                hitSound.Play();
            }

            // Check if the ball hits the left paddle while travelling towards the left.
            if (ball.Direction.X < 0 && ball.CollisionRect.Intersects(leftPaddle.CollisionRect))
            {
                ball.Direction.X *= -1;
                hitSound.Play();
            }

            // Check if the ball hits the right paddle while travelling towards the right.
            if (ball.Direction.X > 0 && ball.CollisionRect.Intersects(rightPaddle.CollisionRect))
            {
                ball.Direction.X *= -1;
                hitSound.Play();
            }
        }


        private void ResetBall()
        {
            // End the game if either player reaches a score of 5.
            if (leftPaddle.Score >= 5 || rightPaddle.Score >= 5)
                gameOver = true;

            // Place the ball in the center of the screen.
            ball.Position = new Vector2(WINDOW_WIDTH / 2f, WINDOW_HEIGHT / 2f);

            // Set the initial direction to a random direction between -60 and 60 degrees.
            float initialAngle = MathHelper.ToRadians(rand.Next(-60, 60));
            ball.Direction = new Vector2((float)Math.Cos(initialAngle), (float)Math.Sin(initialAngle));

            // 50% chance of flipping the direction around to face the left.
            bool facingLeft = (rand.Next(2) == 0);
            if (facingLeft)
                ball.Direction.X *= -1;

            // Set the initial speed.
            ball.Speed = 150;
        }
    }
}
