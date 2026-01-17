using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    internal sealed class ColorTablePickerForm : Form
    {
        private readonly Bitmap bitmap;
        private readonly int columnCount;
        private readonly PictureBox pictureBox;
        private readonly Label selectionLabel;
        private int hoveredColumn = -1;

        public int SelectedIndex1Based { get; private set; }

        public ColorTablePickerForm(Bitmap bitmap, int columnCount, int initialIndex1Based)
        {
            this.bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
            this.columnCount = Math.Max(1, columnCount);
            SelectedIndex1Based = Math.Clamp(initialIndex1Based, 1, this.columnCount);

            Text = "Pick Color Table Index";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(520, 420);

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = bitmap,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            pictureBox.Paint += pictureBox_Paint;
            pictureBox.MouseMove += pictureBox_MouseMove;
            pictureBox.MouseLeave += pictureBox_MouseLeave;
            pictureBox.MouseDown += pictureBox_MouseDown;

            selectionLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0)
            };
            UpdateSelectionLabel();

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Width = 80
            };
            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Width = 80
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8, 6, 8, 6),
                WrapContents = false
            };
            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);

            Controls.Add(pictureBox);
            Controls.Add(selectionLabel);
            Controls.Add(buttonPanel);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            DoubleBuffered = true;
        }

        private void pictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (!TryGetImagePoint(pictureBox, e.Location, out var imgPt))
            {
                return;
            }

            int col = GetColumnFromImageX(imgPt.X);
            SelectedIndex1Based = col + 1;
            UpdateSelectionLabel();
            pictureBox.Invalidate();
        }

        private void pictureBox_MouseLeave(object? sender, EventArgs e)
        {
            if (hoveredColumn != -1)
            {
                hoveredColumn = -1;
                pictureBox.Invalidate();
            }
        }

        private void pictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!TryGetImagePoint(pictureBox, e.Location, out var imgPt))
            {
                if (hoveredColumn != -1)
                {
                    hoveredColumn = -1;
                    pictureBox.Invalidate();
                }
                return;
            }

            int col = GetColumnFromImageX(imgPt.X);
            if (col != hoveredColumn)
            {
                hoveredColumn = col;
                pictureBox.Invalidate();
            }
        }

        private void pictureBox_Paint(object? sender, PaintEventArgs e)
        {
            if (pictureBox.Image == null)
            {
                return;
            }

            if (!TryGetImageRect(pictureBox, out var imgRect))
            {
                return;
            }

            using var gridPen = new Pen(Color.FromArgb(100, Color.White), 1);
            for (int i = 1; i < columnCount; i++)
            {
                float x = imgRect.Left + (imgRect.Width * (i / (float)columnCount));
                e.Graphics.DrawLine(gridPen, x, imgRect.Top, x, imgRect.Bottom);
            }

            int selectedCol = Math.Clamp(SelectedIndex1Based - 1, 0, columnCount - 1);
            float colWidth = imgRect.Width / (float)columnCount;
            var selectedRect = new RectangleF(imgRect.Left + selectedCol * colWidth, imgRect.Top, colWidth, imgRect.Height);
            using var selPen = new Pen(Color.Lime, 2);
            e.Graphics.DrawRectangle(selPen, selectedRect.X, selectedRect.Y, selectedRect.Width, selectedRect.Height);

            if (hoveredColumn >= 0 && hoveredColumn < columnCount && hoveredColumn != selectedCol)
            {
                var hoverRect = new RectangleF(imgRect.Left + hoveredColumn * colWidth, imgRect.Top, colWidth, imgRect.Height);
                using var hoverPen = new Pen(Color.FromArgb(200, Color.Yellow), 2);
                e.Graphics.DrawRectangle(hoverPen, hoverRect.X, hoverRect.Y, hoverRect.Width, hoverRect.Height);
            }
        }

        private int GetColumnFromImageX(int imageX)
        {
            float colWidth = bitmap.Width / (float)columnCount;
            int col = (int)Math.Floor(imageX / colWidth);
            return Math.Clamp(col, 0, columnCount - 1);
        }

        private void UpdateSelectionLabel()
        {
            selectionLabel.Text = $"Index: {SelectedIndex1Based} / {columnCount} (click a column to select)";
        }

        private static bool TryGetImageRect(PictureBox pb, out RectangleF rect)
        {
            rect = default;
            if (pb.Image == null)
            {
                return false;
            }

            var img = pb.Image;
            float imgAspect = img.Width / (float)img.Height;
            float boxAspect = pb.ClientSize.Width / (float)Math.Max(1, pb.ClientSize.Height);

            if (boxAspect > imgAspect)
            {
                float height = pb.ClientSize.Height;
                float width = height * imgAspect;
                float x = (pb.ClientSize.Width - width) / 2f;
                rect = new RectangleF(x, 0, width, height);
                return true;
            }
            else
            {
                float width = pb.ClientSize.Width;
                float height = width / imgAspect;
                float y = (pb.ClientSize.Height - height) / 2f;
                rect = new RectangleF(0, y, width, height);
                return true;
            }
        }

        private static bool TryGetImagePoint(PictureBox pb, Point clientPoint, out Point imagePoint)
        {
            imagePoint = default;
            if (pb.Image == null || !TryGetImageRect(pb, out var imgRect))
            {
                return false;
            }

            if (!imgRect.Contains(clientPoint))
            {
                return false;
            }

            float u = (clientPoint.X - imgRect.Left) / imgRect.Width;
            float v = (clientPoint.Y - imgRect.Top) / imgRect.Height;

            int x = (int)Math.Round(u * (pb.Image.Width - 1));
            int y = (int)Math.Round(v * (pb.Image.Height - 1));
            imagePoint = new Point(Math.Clamp(x, 0, pb.Image.Width - 1), Math.Clamp(y, 0, pb.Image.Height - 1));
            return true;
        }
    }
}
