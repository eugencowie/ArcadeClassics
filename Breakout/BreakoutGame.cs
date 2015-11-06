using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Breakout
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
        public int Lives;

        public Rectangle CollisionRect {
            get { return new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height); }
        }
    }


    public class BreakoutGame : Game
    {
        const int WINDOW_WIDTH = 720;
        const int WINDOW_HEIGHT = 600;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Texture2D backgroundTexture;
        SpriteFont livesFont;
        SoundEffect hitSound;

        Paddle paddle;
        Ball ball;

        Color?[,] bricks = new Color?[10, 10];
        Texture2D brickTexture;

        Random rand = new Random();

        bool gameOver;
        bool anyBricks;

        int level;


        public BreakoutGame()
        {
            graphics = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = WINDOW_WIDTH,
                PreferredBackBufferHeight = WINDOW_HEIGHT
            };
            Content.RootDirectory = "Content";
        }


        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load content.
            backgroundTexture = Content.Load<Texture2D>("textures/background");
            brickTexture = Content.Load<Texture2D>("textures/brick");
            paddle.Texture = Content.Load<Texture2D>("textures/paddle");
            ball.Texture = Content.Load<Texture2D>("textures/ball");

            livesFont = Content.Load<SpriteFont>("fonts/livesfont");
            hitSound = Content.Load<SoundEffect>("sounds/hit");
            
            // Load level 1.
            LoadNextLevel();

            // Set initial paddle state.
            paddle.Position = new Vector2(
                (WINDOW_WIDTH - ball.Texture.Width) / 2f,
                (WINDOW_HEIGHT - ball.Texture.Height) - 40);
            paddle.Lives = 5;

            // Set initial ball state.
            ResetBall();
        }


        protected override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            if (!gameOver)
            {
                CheckPaddleInput(delta);
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
                // Draw background.
                spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT), Color.White);

                // Draw bricks.
                anyBricks = false;
                for (int y = 0; y < bricks.GetLength(1); y++)
                {
                    for (int x = 0; x < bricks.GetLength(0); x++)
                    {
                        if (!bricks[x, y].HasValue)
                            continue;

                        spriteBatch.Draw(brickTexture, new Vector2(brickTexture.Width * x, brickTexture.Height * y), bricks[x, y].Value);
                        anyBricks = true;
                    }
                }

                // Check for level complete condition.
                if (!anyBricks)
                {
                    paddle.Score += 100;
                    LoadNextLevel();
                    ResetBall();
                }

                // Draw paddle.
                spriteBatch.Draw(paddle.Texture, paddle.Position, Color.White);

                // Draw ball.
                spriteBatch.Draw(ball.Texture, ball.Position, Color.White);

                // Draw the player's lives and score.
                spriteBatch.DrawString(livesFont, "Lives: " + paddle.Lives, new Vector2(10, 5), Color.White);
                spriteBatch.DrawString(livesFont, "Score: " + paddle.Score, new Vector2(WINDOW_WIDTH - 170, 5), Color.White);
            }
            else
            {
                // The game is over.
                string text = (!anyBricks ? "Congratulations, player! You won!" : "Bad luck, player! You ran out of lives!");
                DrawCenteredText(spriteBatch, livesFont, text);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }


        #region Update functions


        private void CheckPaddleInput(float delta)
        {
            KeyboardState keyboard = Keyboard.GetState();

            // Check if the player wants to move left.
            if (keyboard.IsKeyDown(Keys.Left) && paddle.Position.X > 0)
                paddle.Position.X -= 500 * delta;

            // Check if the player wants to move right.
            if (keyboard.IsKeyDown(Keys.Right) && paddle.Position.X < WINDOW_WIDTH - paddle.Texture.Width)
                paddle.Position.X += 500 * delta;
        }


        private void UpdateBall(float delta)
        {
            // Update the ball's position based on its direction and speed.
            ball.Position += (ball.Direction * ball.Speed) * delta;

            // Slowly increase the ball's speed over time.
            ball.Speed += 20 * delta;

            // Check if the ball leaves the screen on the bottom.
            if (ball.Position.Y >= WINDOW_HEIGHT)
            {
                paddle.Lives--;
                ResetBall();
            }

            // Check if the ball hits the top of the screen.
            if (ball.Position.Y <= 0)
            {
                ball.Direction.Y *= -1;
            }

            // Check if the ball hits the left or right of the screen.
            if (ball.Position.X <= 0 || ball.Position.X + ball.Texture.Width >= WINDOW_WIDTH)
            {
                ball.Direction.X *= -1;
            }

            // Check if the ball hits the paddle while travelling downwards.
            if (ball.Direction.Y > 0 && ball.CollisionRect.Intersects(paddle.CollisionRect))
            {
                ball.Direction.Y *= -1;
            }

            // Check if the ball hits any of the bricks.
            for (int y = 0; y < bricks.GetLength(1); y++)
            {
                for (int x = 0; x < bricks.GetLength(0); x++)
                {
                    if (!bricks[x, y].HasValue)
                        continue;

                    var collisionRect = new Rectangle(
                        brickTexture.Width * x,
                        brickTexture.Height * y,
                        brickTexture.Width,
                        brickTexture.Height);
                        
                    if (ball.CollisionRect.Intersects(collisionRect))
                    {
                        // Create a rectangle representing a vertical stripe across the screen the height of the brick.
                        var verticalStripe = new Rectangle(
                            collisionRect.X,
                            0,
                            collisionRect.Width,
                            WINDOW_HEIGHT);

                        // Create a rectangle representing a horizontal stripe across the screen the width of the brick.
                        var horizontalStripe = new Rectangle(
                            0,
                            collisionRect.Y,
                            WINDOW_WIDTH,
                            collisionRect.Height);

                        // Get the position of the center of the ball.
                        var ballCenter = new Vector2(
                            ball.Position.X + (ball.Texture.Width / 2f),
                            ball.Position.Y + (ball.Texture.Height / 2f));

                        // If the center of the ball falls within the vertical stripe, reverse the direction of
                        // the ball on the y axis.
                        if (verticalStripe.Contains(ballCenter))
                            ball.Direction.Y *= -1;

                        // If the center of the ball falls within the horizontal stripe, reverse the direction of
                        // the ball on the x axis.
                        if (horizontalStripe.Contains(ballCenter))
                            ball.Direction.X *= -1;

                        // Remove the brick.
                        bricks[x, y] = null;

                        // Play the hit sound.
                        hitSound.Play();

                        // Increase the player's score.
                        paddle.Score += 1;
                    }
                }
            }
        }


        private void ResetBall()
        {
            // End the game if the player runs out of lives.
            if (paddle.Lives <= 0)
                gameOver = true;

            // Place the ball in the center of the screen at the bottom.
            ball.Position = new Vector2(
                (paddle.Position.X + (paddle.Texture.Width / 2f)) - (ball.Texture.Width / 2f),
                (paddle.Position.Y - ball.Texture.Height));

            // Set the initial direction to a random direction between -135 and -45 degrees.
            float initialAngle = MathHelper.ToRadians(rand.Next(-135, -45));
            ball.Direction = new Vector2((float)Math.Cos(initialAngle), (float)Math.Sin(initialAngle));

            // Set the initial speed.
            ball.Speed = 150;
        }


        #endregion

        #region Draw functions


        /// <summary>
        /// Draws text in center of the screen, optionally using the provided offset.
        /// </summary>
        private void DrawCenteredText(SpriteBatch batch, SpriteFont font, string text, Vector2 offset = default(Vector2))
        {
            var textPosition = new Vector2(
                    (WINDOW_WIDTH - font.MeasureString(text).X) / 2f,
                    (WINDOW_HEIGHT - font.MeasureString(text).Y) / 2f);

            batch.DrawString(font, text, textPosition + offset, Color.White);
        }


        #endregion

        #region Levels


        /// <summary>
        /// Loads the next level, or sets the game over condition if all levels have been played.
        /// </summary>
        private void LoadNextLevel()
        {
            level++;

            if (level > 3)
            {
                gameOver = true;
                anyBricks = false;
            }

            switch (level)
            {
                case 1:
                    LoadLevel1();
                    break;

                case 2:
                    LoadLevel2();
                    break;

                case 3:
                    LoadLevel3();
                    break;

                default:
                    LoadLevel1();
                    break;
            }
        }


        private void LoadLevel1()
        {
            // Create lines 6-7 of bricks (center seven).
            for (int y = 6; y <= 7; y++)
                for (int x = 1; x <= 7; x++)
                    bricks[x, y] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));

            // Create line 9 of bricks (center seven).
            for (int x = 1; x <= 7; x++)
                bricks[x, 9] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));
        }


        private void LoadLevel2()
        {
            // Create top line of bricks (center three).
            for (int x = 3; x <= 5; x++)
                bricks[x, 1] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));

            // Create lines 2-3 of bricks (center five).
            for (int y = 2; y <= 3; y++)
                for (int x = 2; x <= 6; x++)
                    bricks[x, y] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));

            // Create lines 4-7 of bricks (center seven).
            for (int y = 4; y <= 7; y++)
                for (int x = 1; x <= 7; x++)
                    bricks[x, y] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));

            // Create line 9 of bricks (center seven).
            for (int x = 1; x <= 7; x++)
                bricks[x, 9] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));
        }


        private void LoadLevel3()
        {
            // Create ALL bricks.
            for (int y = 0; y < bricks.GetLength(1); y++)
                for (int x = 0; x < bricks.GetLength(0); x++)
                    bricks[x, y] = new Color(rand.Next(255), rand.Next(255), rand.Next(255));
        }


        #endregion
    }
}
