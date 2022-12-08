﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InetOptimizer
{
    public class Overlay : Form
    {
        enum Level // need better state, suboverlay type/etc.
        {
            None,
            StatusEffectTimes,
            RaidDamage,
            Damage,
            BuffedAttacks,
            BattleItems,
            Counterattacks,
            Stagger,
            Heal,
            Shield,
            TimeAlive,
            RaidTimeAlive,
            Max
        }
        enum Scope // need better state, suboverlay type/etc.
        {
            TopLevel,
            Encounters,
            Player,
            Skill
        }
        Level level = Level.Damage;
        Scope scope = Scope.TopLevel;

        uint skillId;

        public Overlay()
        {
            InitPens();
            Control.CheckForIllegalCrossThreadCalls = false;
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 120);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimizeBox = false;
            this.Name = "Overlay";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Overlay";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.SystemColors.Control;
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Overlay_MouseDown);
            this.ResumeLayout(false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            Load += Overlay_Load;
        }
        internal void AddSniffer(Parser s)
        {
            sniffer = s;
            sniffer.onCombatEvent += AddDamageEvent;
            sniffer.statusEffectTracker.OnChange += StatusEffectsChangedEvent;
            encounter = sniffer.currentEncounter;
        }
        Encounter encounter;
        Entity SubEntity;
        Font font = new Font("Helvetica", 10);
        bool notCurrentEncounter = false;
        void AddDamageEvent(LogInfo log)
        {
            if (sniffer.currentEncounter.Infos.Count > 0 && !notCurrentEncounter)
            {
                encounter = sniffer.currentEncounter;
                Invalidate();
            }
        }

        void StatusEffectsChangedEvent()
        {
            if(level == Level.StatusEffectTimes)
                Invalidate();
        }

        internal Parser sniffer;
        List<Brush> brushes = new List<Brush>();
        Brush black = new SolidBrush(Color.DarkGray);
        void InitPens()
        {
            String[] colors = { "#3366cc", "#dc3912", "#ff9900", "#109618", "#990099", "#0099c6", "#dd4477", "#66aa00", "#b82e2e", "#316395", "#994499", "#22aa99", "#aaaa11", "#6633cc", "#e67300", "#8b0707", "#651067", "#329262", "#5574a6", "#3b3eac", "#b77322", "#16d620", "#b91383", "#f4359e", "#9c5935", "#a9c413", "#2a778d", "#668d1c", "#bea413", "#0c5922", "#743411" };
            foreach (var color in colors) brushes.Add(new SolidBrush(ColorTranslator.FromHtml(color)));
        }
        int barHeight = 20;
        public static string FormatNumber(UInt64 n) // https://stackoverflow.com/questions/30180672/string-format-numbers-to-millions-thousands-with-rounding
        {
            if (n < 1000) return n.ToString();
            if (n < 10000) return String.Format("{0:#,.##}K", n - 5);
            if (n < 100000) return String.Format("{0:#,.#}K", n - 50);
            if (n < 1000000) return String.Format("{0:#,.}K", n - 500);
            if (n < 10000000) return String.Format("{0:#,,.##}M", n - 5000);
            if (n < 100000000) return String.Format("{0:#,,.#}M", n - 50000);
            if (n < 1000000000) return String.Format("{0:#,,.}M", n - 500000);
            return String.Format("{0:#,,,.##}B", n - 5000000);
        }

        public Rectangle GetSpriteLocation(int i)
        {
            i--;
            var imageSize = 64;
            var x = i % 17;
            var y = i / 17;
            return new Rectangle(x * imageSize, y * imageSize, imageSize, imageSize);
        }
        public String[] ClassIconIndex = { "start1", "Destroyer", "unk", "Arcana", "Berserker", "Wardancer", "Deadeye", "MartialArtist", "Gunlancer", "Gunner", "Scrapper", "Mage", "Summoner", "Warrior",
         "Soulfist", "Sharpshooter", "Artillerist", "dummyfill", "Bard", "Glavier", "Assassin", "Deathblade", "Shadowhunter", "Paladin", "Scouter", "Reaper", "FemaleGunner", "Gunslinger", "MaleMartialArtist", "Striker", "Sorceress" };
        public Pen arrowPen = new Pen(Color.FromArgb(255, 255, 255, 255), 4) { StartCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor };
        IOrderedEnumerable<KeyValuePair<String, Tuple<UInt64, UInt32, UInt32, UInt64>>> orderedRows;
        private readonly Bitmap ClassSymbols = Properties.Resources.class_symbol_0;
        protected void OnPaintStatusEffectTimes(PaintEventArgs e, float heightBuffer)
        {
            var rows = scope == Scope.Player ? encounter.GetStatusEffects(SubEntity) : encounter.GetStatusEffects();
            var elapsed = ((encounter.End == default(DateTime) ? DateTime.Now : encounter.End) - encounter.Start).TotalSeconds;
            orderedRows = rows.OrderByDescending(a => a.Value);
            for (var i = 0; i < orderedRows.Count(); i++)
            {
                var rowData = orderedRows.ElementAt(i);
                int barWidth = (int)((rowData.Value.Item1 / elapsed) * Size.Width);
                var nameOffset = 0;
                var infoString = $"{rowData.Value.Item1}s {((rowData.Value.Item1 * 100) / elapsed):0.#}%";
                e.Graphics.FillRectangle(brushes[i % brushes.Count], 0, (i + 1) * barHeight, barWidth, barHeight);
                if (rowData.Key.Contains('(') && scope == Scope.TopLevel)
                {
                    var className = rowData.Key.Substring(rowData.Key.IndexOf("(") + 1);
                    className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                    //var className = rowData.Key[(rowData.Key.IndexOf("(") + 1)..];
                    //className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                    e.Graphics.DrawImage(ClassSymbols, new Rectangle(2, (i + 1) * barHeight + 2, barHeight - 4, barHeight - 4), GetSpriteLocation(Array.IndexOf(ClassIconIndex, className)), GraphicsUnit.Pixel);
                    nameOffset += 2 + barHeight - 4;
                }
                var edge = e.Graphics.MeasureString(infoString, font);
                e.Graphics.DrawString(rowData.Key, font, black, nameOffset + 5, (i + 1) * barHeight + heightBuffer);
                e.Graphics.DrawString(infoString, font, black, Size.Width - edge.Width, (i + 1) * barHeight + heightBuffer);
            }
        }

        protected void OnPaintBuffedAttacks(PaintEventArgs e, float heightBuffer)
        {
            if (scope == Scope.Skill)
            {
                var rows = encounter.GetHits(SubEntity, skillId);
                var elapsed = ((encounter.End == default(DateTime) ? DateTime.Now : encounter.End) - encounter.Start).TotalSeconds;
                var totalDamage = rows.Values.Sum(b => (Single)b.Item1);
                orderedRows = rows.OrderByDescending(a => a.Value.Item1);
                for (var i = 0; i < orderedRows.Count(); i++)
                {
                    var rowData = orderedRows.ElementAt(i);
                    int barWidth = (int)((rowData.Value.Item1 / totalDamage) * Size.Width);
                    var nameOffset = 0;
                    var infoString = $"{FormatNumber(rowData.Value.Item1)} Attk: {rowData.Value.Item2} Debuff: {1f * rowData.Value.Item4}";
                    e.Graphics.FillRectangle(brushes[i % brushes.Count], 0, (i + 1) * barHeight, barWidth, barHeight);
                    if (rowData.Key.Contains('(') && scope == Scope.TopLevel)
                    {
                        var className = rowData.Key.Substring(rowData.Key.IndexOf("(") + 1);
                        className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                        //var className = rowData.Key[(rowData.Key.IndexOf("(") + 1)..];
                        //className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                        e.Graphics.DrawImage(ClassSymbols, new Rectangle(2, (i + 1) * barHeight + 2, barHeight - 4, barHeight - 4), GetSpriteLocation(Array.IndexOf(ClassIconIndex, className)), GraphicsUnit.Pixel);
                        nameOffset += 2 + barHeight - 4;
                    }
                    var edge = e.Graphics.MeasureString(infoString, font);
                    e.Graphics.DrawString(rowData.Key, font, black, nameOffset + 5, (i + 1) * barHeight + heightBuffer);
                    e.Graphics.DrawString(infoString, font, black, Size.Width - edge.Width, (i + 1) * barHeight + heightBuffer);
                }
            }
            else
            {
                var rows = scope == Scope.Player ? encounter.GetAttackBuffStats((i => (float)(i.Damage)), SubEntity) : encounter.GetAttackBuffStats((i => (float)(i.Damage)));
                var elapsed = ((encounter.End == default(DateTime) ? DateTime.Now : encounter.End) - encounter.Start).TotalSeconds;
                var totalDamage = rows.Values.Sum(b => (Single)b.Item1);
                orderedRows = rows.ToDictionary(a => a.Key, a => Tuple.Create<UInt64, UInt32, UInt32, UInt64>(a.Value.Item1, a.Value.Item2, a.Value.Item3, a.Value.Item4)).OrderByDescending(a => a.Value.Item1);
                var fullOrderedRows = rows.OrderByDescending(a => a.Value.Item1);
                for (var i = 0; i < orderedRows.Count(); i++)
                {
                    var rowData = fullOrderedRows.ElementAt(i);
                    int barWidth = (int)((rowData.Value.Item1 / totalDamage) * Size.Width);
                    var nameOffset = 0;
                    var infoString = $"{FormatNumber((ulong)(rowData.Value.Item1 / elapsed))} {((rowData.Value.Item1) / totalDamage):P1} Att: {(1f * rowData.Value.Item3 / rowData.Value.Item2):P1} {(1f * rowData.Value.Item5 / rowData.Value.Item1):P1} Debuff: {(1f * rowData.Value.Item4 / rowData.Value.Item2):P1} {(1f * rowData.Value.Item6 / rowData.Value.Item1):P1}";
                    e.Graphics.FillRectangle(brushes[i % brushes.Count], 0, (i + 1) * barHeight, barWidth, barHeight);
                    if (rowData.Key.Contains('(') && scope == Scope.TopLevel)
                    {
                        var className = rowData.Key.Substring(rowData.Key.IndexOf("(") + 1);
                        className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                        //var className = rowData.Key[(rowData.Key.IndexOf("(") + 1)..];
                        //className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                        e.Graphics.DrawImage(ClassSymbols, new Rectangle(2, (i + 1) * barHeight + 2, barHeight - 4, barHeight - 4), GetSpriteLocation(Array.IndexOf(ClassIconIndex, className)), GraphicsUnit.Pixel);
                        nameOffset += 2 + barHeight - 4;
                    }
                    var edge = e.Graphics.MeasureString(infoString, font);
                    e.Graphics.DrawString(rowData.Key, font, black, nameOffset + 5, (i + 1) * barHeight + heightBuffer);
                    e.Graphics.DrawString(infoString, font, black, Size.Width - edge.Width, (i + 1) * barHeight + heightBuffer);
                }
            }
        }

        protected float OnPaintDrawTitleBar(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(brushes[10], 0, 0, Size.Width, barHeight);

            var title = "DPS Meter";
            if (scope == Scope.Encounters) title = "Encounters";
            else
            {
                title = level.ToString();
                if (scope == Scope.Player) title += " (" + SubEntity.VisibleName + ")";
            }
            var bighHealthNpcs = encounter.BigNPCHealthMap.Where(x => (DateTime.UtcNow - x.Value.Item1).TotalSeconds < 30).ToArray();
            if (bighHealthNpcs.Length > 0)
            {
                title += " " + bighHealthNpcs.Select(x => $"{x.Key}: {x.Value.Item2}% of {FormatNumber((ulong)x.Value.Item3)}").Aggregate((x, y) => $"{x} - {y}");
            }
            var titleBar = e.Graphics.MeasureString(title, font);
            var heightBuffer = (barHeight - titleBar.Height) / 2;
            e.Graphics.DrawString(title, font, black, 5, heightBuffer);

            ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(Size.Width - 50, barHeight / 4, 10, barHeight / 2));
            e.Graphics.DrawLine(arrowPen, Size.Width - 30, barHeight / 2, Size.Width - 20, barHeight / 2);
            e.Graphics.DrawLine(arrowPen, Size.Width - 5, barHeight / 2, Size.Width - 15, barHeight / 2);
            ControlPaint.DrawSizeGrip(e.Graphics, BackColor, ClientSize.Width - 16, ClientSize.Height - 16, 16, 16);
            return heightBuffer;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            var heightBuffer = OnPaintDrawTitleBar(e);



            if (scope == Scope.Encounters)
            {
                for (var i = 0; i < sniffer.Encounters.Count; i++)
                {
                    e.Graphics.FillRectangle(brushes[i % brushes.Count], 0, (i + 1) * barHeight, Size.Width, barHeight);
                    e.Graphics.DrawString(sniffer.Encounters.ElementAt(sniffer.Encounters.Count - i - 1).EncounterName, font, black, 5, (i + 1) * barHeight + heightBuffer);
                }
            }
            else
            {
                switch (level)
                {
                    case Level.StatusEffectTimes:
                        OnPaintStatusEffectTimes(e, heightBuffer);
                        return;
                    case Level.BuffedAttacks:
                        OnPaintBuffedAttacks(e, heightBuffer);
                        return;
                }
                var elapsed = ((encounter.End == default(DateTime) ? DateTime.Now : encounter.End) - encounter.Start).TotalSeconds;
                var rows = encounter.GetDamages((i => (Single)(
                    level == Level.Damage ? i.Damage :
                    level == Level.RaidDamage ? i.Damage :
                    level == Level.Stagger ? i.Stagger :
                    level == Level.Counterattacks ? (i.Counter ? 1u : 0) :
                    level == Level.TimeAlive ? (i.TimeAlive) :
                    level == Level.RaidTimeAlive ? (i.TimeAlive) :
                    level == Level.Heal ? i.Heal :
                    level == Level.Shield ? i.Shield : 0)), SubEntity);
                if (level == Level.Damage) rows = encounter.GetDamages((i => i.Damage), SubEntity);
                else if (level == Level.Counterattacks) rows = encounter.Counterattacks.ToDictionary(x => x.Key, x => Tuple.Create(x.Value, 0u, 0u, 0ul));
                else if (level == Level.Stagger) rows = encounter.Stagger.ToDictionary(x => x.Key, x => Tuple.Create(x.Value, 0u, 0u, 0ul));
                else if (level == Level.TimeAlive) rows = encounter.TimeAlive.ToDictionary(x => x.Key, x => Tuple.Create(x.Value, 0u, 0u, 0ul));
                else if (level == Level.BattleItems) rows = encounter.GetBattleItems((i => i.BattleItem ? 1 : 0), SubEntity);
                else if (level == Level.RaidTimeAlive)
                {
                    rows = encounter.RaidTimeAlive.ToDictionary(x => x.Key, x => Tuple.Create(x.Value, 0u, 0u, 0ul));
                    elapsed = encounter.RaidTime;
                }
                else if (level == Level.RaidDamage)
                {
                    rows = encounter.GetRaidDamages((i => i.Damage), SubEntity);
                    elapsed = encounter.RaidTime;
                }
                if (level == Level.Damage && scope == Scope.Skill)
                {
                    rows = encounter.GetHits(SubEntity, skillId);
                }
                var maxDamage = rows.Count == 0 ? 0 : rows.Max(b => b.Value.Item1);
                var totalDamage = rows.Values.Sum(b => (Single)b.Item1);
                orderedRows = rows.OrderByDescending(b => b.Value);
                for (var i = 0; i < orderedRows.Count(); i++)
                {
                    var playerDmg = orderedRows.ElementAt(i);
                    var rowText = playerDmg.Key;
                    if (scope == Scope.Skill)
                    {
                        rowText = rowText.Substring(rowText.IndexOf(":")+2);
                    }
                    var barWidth = ((Single)playerDmg.Value.Item1 / maxDamage) * Size.Width;
                    //if (barWidth < .3f) continue;
                    e.Graphics.FillRectangle(brushes[i % brushes.Count], 0, (i + 1) * barHeight, barWidth, barHeight);
                    var dps = FormatNumber((ulong)(playerDmg.Value.Item1 / elapsed));
                    if (playerDmg.Value.Item4 > 0)
                    {
                        dps = FormatNumber((ulong)(playerDmg.Value.Item1 / playerDmg.Value.Item4));
                    }


                    var formattedDmg = FormatNumber(playerDmg.Value.Item1) + " (" + dps + ", " + (1f * playerDmg.Value.Item1 / totalDamage).ToString("P1") + ")";
                    if (level == Level.TimeAlive || level == Level.RaidTimeAlive)
                    {
                        dps = (1f * playerDmg.Value.Item1 / totalDamage).ToString("P1");
                        formattedDmg = FormatNumber(playerDmg.Value.Item1) + " (" + dps + ", " + (1f * playerDmg.Value.Item1 / elapsed).ToString("P1") + ")";
                    }
                    if ((level == Level.Damage || level == Level.RaidDamage) && Width > 450)
                    {
                        formattedDmg += " | H: " + playerDmg.Value.Item2 + " | C: " + (1f * playerDmg.Value.Item3 / playerDmg.Value.Item2).ToString("P1");
                    }
                    var nameOffset = 0;
                    if (rowText.Contains("(") && scope == Scope.TopLevel)
                    {
                        var className = rowText.Substring(rowText.IndexOf("(") + 1);
                        className = className.Substring(0, className.IndexOf(")")).Split(' ')[1];
                        e.Graphics.DrawImage(ClassSymbols, new Rectangle(2, (i + 1) * barHeight + 2, barHeight - 4, barHeight - 4), GetSpriteLocation(Array.IndexOf(ClassIconIndex, className)), GraphicsUnit.Pixel);
                        nameOffset += 16;
                    }
                    if (rowText.Contains("(") && scope == Scope.Player)
                    {
                        /*if (Skill.GetSkillIcon(uint.Parse(rowText), out String iconFile, out int iconIndex))
                        {
                            nameOffset += 16;
                            e.Graphics.DrawImage((Bitmap)Properties.Resources.ResourceManager.GetObject(iconFile.ToLower()), new Rectangle(2, (i + 1) * barHeight + 2, barHeight - 4, barHeight - 4), GetSpriteLocation(iconIndex), GraphicsUnit.Pixel);
                        }*/
                        string skillid;
                        string skillIdPart;
                        skillIdPart = rowText.Substring(1, rowText.IndexOf(")") - 1);
                        var ids = skillIdPart.Split(",");
                        skillid = ids[0];
                        var skillEffectId = ids[1];
                        rowText = Skill.GetSkillName(uint.Parse(skillid), uint.Parse(skillEffectId));
                    }
                    var edge = e.Graphics.MeasureString(formattedDmg, font);
                    e.Graphics.DrawString(rowText, font, black, nameOffset + 5, (i + 1) * barHeight + heightBuffer);
                    e.Graphics.DrawString(formattedDmg, font, black, Size.Width - edge.Width, (i + 1) * barHeight + heightBuffer);
                }
            }
        }
        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        void Overlay_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);

                var index = (int)Math.Floor(e.Location.Y / (float)barHeight - 1);
                if (index >= 0)// && index <= Damages.Count)
                {
                    if (scope == Scope.TopLevel)
                    {
                        string entityName;
                        switch (level)
                        {
                            case Level.StatusEffectTimes:
                                entityName = orderedRows.ElementAt(index).Key;
                                SubEntity = encounter.Infos.First(i => i.DestinationEntity.VisibleName == entityName).DestinationEntity;
                                SwitchOverlay(Scope.Player);
                                break;
                            default:
                                entityName = orderedRows.ElementAt(index).Key;
                                var p = encounter.Infos.FirstOrDefault(i => i.SourceEntity.VisibleName == entityName);
                                if (p != default(LogInfo))
                                    SubEntity = p.SourceEntity;
                                SwitchOverlay(Scope.Player);
                                break;
                        }

                    }
                    else if (scope == Scope.Encounters)
                    {
                        encounter = sniffer.Encounters.ElementAt(sniffer.Encounters.Count - index - 1);
                        notCurrentEncounter = (index > 0);
                        SwitchOverlay(Scope.TopLevel);
                    }
                    else if (scope == Scope.Player && (level == Level.Damage || level == Level.BuffedAttacks))
                    {
                        string elementKey = orderedRows.ElementAt(index).Key;
                        string skillid;
                        string skillIdPart;
                        skillIdPart = elementKey.Substring(1, elementKey.IndexOf(")") - 1);
                        var ids = skillIdPart.Split(",");
                        skillid = ids[0];
                        this.skillId = uint.Parse(skillid);
                        SwitchOverlay(Scope.Skill);
                    }
                }
                if (new Rectangle(Size.Width - 50, barHeight / 4, 10, barHeight / 2).Contains(e.Location)) SwitchOverlay(Scope.Encounters);
                if (new Rectangle(Size.Width - 30, 0, 10, barHeight).Contains(e.Location)) SwitchOverlay(false);
                if (new Rectangle(Size.Width - 15, 0, 10, barHeight).Contains(e.Location)) SwitchOverlay(true);
            }
            if (e.Button == MouseButtons.Right)
            {
                if (scope == Scope.Player)
                    SwitchOverlay(Scope.TopLevel);
                else if (scope == Scope.TopLevel)
                    SwitchOverlay(Scope.Encounters);
                else if (scope == Scope.Skill)
                    SwitchOverlay(Scope.Player);
            }
        }
        void SwitchOverlay(bool progress)
        {
            if (progress) level++;
            else level--;
            if (level == Level.None) level = Level.Max - 1;
            else if (level == Level.Max) level = Level.None + 1;
            scope = Scope.TopLevel;
            SubEntity = null;

            Invalidate();
        }
        void SwitchOverlay(Level type)
        {
            level = type;
            scope = Scope.TopLevel;
            Invalidate();
        }
        void SwitchOverlay(Scope type)
        {
            if (type != Scope.Player && type != Scope.Skill) SubEntity = null;
            scope = type;
            Invalidate();
        }
        protected override void WndProc(ref Message m)
        {
            const int wmNcHitTest = 0x84;
            const int htBottomLeft = 16;
            const int htBottomRight = 17;
            const int WM_EXITSIZEMOVE = 0x232; // catch stop moving
            if (m.Msg == wmNcHitTest)
            {
                var x = (int)(m.LParam.ToInt64() & 0xFFFF);
                var y = (int)((m.LParam.ToInt64() & 0xFFFF0000) >> 16);
                var pt = PointToClient(new Point(x, y));
                var clientSize = ClientSize;
                if (pt.X >= clientSize.Width - 16 && pt.Y >= clientSize.Height - 16 && clientSize.Height >= 16)
                {
                    m.Result = (IntPtr)(IsMirrored ? htBottomLeft : htBottomRight);
                    return;
                }
            }
            if (m.Msg == WM_EXITSIZEMOVE)
            {
                Properties.Settings.Default.F1Location = this.Location;
                Properties.Settings.Default.F1Size = this.Size;
                Properties.Settings.Default.Save();
            }
            base.WndProc(ref m);
        }

        public new void Dispose()
        {
            sniffer.onCombatEvent -= AddDamageEvent;
            base.Dispose();
        }

        private void Overlay_Load(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.F1Location;
            this.Size = Properties.Settings.Default.F1Size;
            this.TopMost = true;
        }

    }
}