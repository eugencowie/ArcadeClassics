using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceInvaders
{
    class Sprite
    {
        public Texture2D Texture;
        public Vector2 Position;
        public bool Alive;

        public Rectangle Bounds
        {
            get
            {
                // Textures are rendered with the center of the texture as the origin, so we need to
                // subtract half of the texture's width and height from the position in order to get
                // the top-left corner of the bounding box.
                return new Rectangle(
                    (int)(Position.X - (Texture.Width / 2f)),
                    (int)(Position.Y - (Texture.Height / 2f)),
                    Texture.Width,
                    Texture.Height);
            }
        }

        public Sprite()
        {
        }

        public Sprite(Texture2D texture, Vector2 position, bool alive=true)
        {
            Texture = texture;
            Position = position;
            Alive = alive;
        }

        public void Draw(SpriteBatch spriteBatch, Color? color=null)
        {
            if (Alive)
            {
                spriteBatch.Draw(
                    Texture,
                    Position,
                    null,
                    color ?? Color.White,
                    0f,
                    new Vector2(Texture.Width / 2f, Texture.Height / 2f), // draw with the origin at the center of the texture
                    1f,
                    SpriteEffects.None,
                    1f);
            }
        }
    }
}
