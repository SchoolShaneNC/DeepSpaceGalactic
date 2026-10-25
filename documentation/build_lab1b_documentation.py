from pathlib import Path
from datetime import date

from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.enum.section import WD_SECTION, WD_ORIENT
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


ROOT = Path(__file__).resolve().parent
DIAGRAMS = ROOT / "uml_sources"
OUTPUT = ROOT / "Lab1B_Documentation.docx"

NAVY = "17365D"
BLUE = "1F4E78"
PALE_BLUE = "EAF2F8"
PALE_GREY = "F4F6F7"
GRID = "D9E2F3"
BLACK = RGBColor(0, 0, 0)


def font(size, bold=False):
    for candidate in ("arialbd.ttf" if bold else "arial.ttf", "DejaVuSans-Bold.ttf" if bold else "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(candidate, size)
        except OSError:
            pass
    return ImageFont.load_default()


def draw_arrow(draw, start, end, triangle=True, fill="#17365D", width=4):
    draw.line([start, end], fill=fill, width=width)
    if not triangle:
        return
    ex, ey = end
    sx, sy = start
    dx, dy = ex - sx, ey - sy
    length = max((dx * dx + dy * dy) ** 0.5, 1)
    ux, uy = dx / length, dy / length
    px, py = -uy, ux
    base_x, base_y = ex - ux * 26, ey - uy * 26
    points = [(ex, ey), (base_x + px * 14, base_y + py * 14), (base_x - px * 14, base_y - py * 14)]
    draw.polygon(points, fill="white", outline=fill)


def draw_dependency(draw, start, end, fill="#17365D", width=3):
    """Draw a UML-style dashed dependency with an open arrowhead."""
    sx, sy = start
    ex, ey = end
    dx, dy = ex - sx, ey - sy
    length = max((dx * dx + dy * dy) ** 0.5, 1)
    ux, uy = dx / length, dy / length
    for distance in range(0, int(length - 18), 16):
        a = distance
        b = min(distance + 9, length - 18)
        draw.line((sx + ux * a, sy + uy * a, sx + ux * b, sy + uy * b), fill=fill, width=width)
    px, py = -uy, ux
    base_x, base_y = ex - ux * 18, ey - uy * 18
    draw.line((ex, ey, base_x + px * 8, base_y + py * 8), fill=fill, width=width)
    draw.line((ex, ey, base_x - px * 8, base_y - py * 8), fill=fill, width=width)


def uml_box(draw, x, y, w, title, attrs, ops, stereotype=None, scale=1):
    title_font = font(25 * scale, True)
    body_font = font(19 * scale)
    small_font = font(16 * scale)
    line_h = 30 * scale
    title_h = 43 * scale + (20 * scale if stereotype else 0)
    h = title_h + max(1, len(attrs)) * line_h + max(1, len(ops)) * line_h + 20 * scale
    # shadow then white box
    draw.rounded_rectangle((x + 5 * scale, y + 5 * scale, x + w + 5 * scale, y + h + 5 * scale), radius=8 * scale, fill="#C9D5E3")
    draw.rounded_rectangle((x, y, x + w, y + h), radius=8 * scale, fill="white", outline="#17365D", width=3 * scale)
    draw.rectangle((x, y, x + w, y + title_h), fill="#D9EAF7", outline="#17365D", width=3 * scale)
    if stereotype:
        bb = draw.textbbox((0, 0), stereotype, font=small_font)
        draw.text((x + (w - (bb[2] - bb[0])) / 2, y + 5 * scale), stereotype, font=small_font, fill="#17365D")
        title_y = y + 22 * scale
    else:
        title_y = y + 8 * scale
    bb = draw.textbbox((0, 0), title, font=title_font)
    draw.text((x + (w - (bb[2] - bb[0])) / 2, title_y), title, font=title_font, fill="#17365D")
    split1 = y + title_h + max(1, len(attrs)) * line_h + 4 * scale
    draw.line((x, split1, x + w, split1), fill="#17365D", width=2 * scale)
    cursor = y + title_h + 7 * scale
    for value in attrs or [""]:
        draw.text((x + 12 * scale, cursor), value, font=body_font, fill="#111111")
        cursor += line_h
    cursor = split1 + 7 * scale
    for value in ops or [""]:
        draw.text((x + 12 * scale, cursor), value, font=body_font, fill="#111111")
        cursor += line_h
    return (x, y, x + w, y + h)


def label(draw, pos, text, size=17, fill="#17365D"):
    draw.text(pos, text, font=font(size), fill=fill)


def build_domain_diagram(path):
    img = Image.new("RGB", (1600, 1160), "white")
    d = ImageDraw.Draw(img)
    d.text((55, 35), "UML Class Diagram - Game Objects and Inheritance", font=font(31, True), fill="#17365D")
    gamepiece = uml_box(d, 600, 105, 400, "GamePiece", ["- objectMargins: Thickness", "- onScreen: Image", "+ Img, Position: Image/Thickness"], ["+ GamePiece(img: Image)", "+ Move(horizontal, vertical): bool", "+ Move(direction: VirtualKey): bool"])
    player = uml_box(d, 55, 475, 390, "Player", ["- lives, health, fireRate", "+ Score, Speed, Damage", "+ PropertyChanged event"], ["+ Player(img: Image)", "+ Player(img, lives, health, ... )"], "<<INotifyPropertyChanged>>")
    enemy = uml_box(d, 605, 475, 390, "Enemy", ["- health, pointValue, speed", "+ Damage, FireRate, Size"], ["+ Enemy(img: Image)"])
    asteroid = uml_box(d, 1150, 475, 390, "Asteroid", ["- velocityX, velocityY", "+ Health, Damage, PointValue"], ["+ Asteroid(img: Image)"])
    projectile = uml_box(d, 1150, 830, 390, "Projectile", ["- velocityX, velocityY", "+ Damage, IsPlayerProjectile"], ["+ Projectile(img: Image)", "+ Projectile(img, velocityX, velocityY, ...)"])
    small = uml_box(d, 55, 860, 280, "SmallEnemy", [], ["+ SmallEnemy(img: Image)"])
    medium = uml_box(d, 390, 860, 280, "MediumEnemy", [], ["+ MediumEnemy(img: Image)"])
    large = uml_box(d, 725, 860, 280, "LargeEnemy", [], ["+ LargeEnemy(img: Image)"])
    draw_arrow(d, (250, player[1]), (670, gamepiece[3]), True)
    draw_arrow(d, (800, enemy[1]), (800, gamepiece[3]), True)
    draw_arrow(d, (1345, asteroid[1]), (930, gamepiece[3]), True)
    draw_arrow(d, (1345, projectile[1]), (960, gamepiece[3]), True)
    draw_arrow(d, (195, small[1]), (670, enemy[3]), True)
    draw_arrow(d, (530, medium[1]), (800, enemy[3]), True)
    draw_arrow(d, (865, large[1]), (930, enemy[3]), True)
    label(d, (55, 1120), "Hollow triangle = generalization (inherits from). Visibility: + public, - private.", 16)
    img.save(path, quality=95)


def build_session_diagram(path):
    img = Image.new("RGB", (1600, 1300), "white")
    d = ImageDraw.Draw(img)
    d.text((55, 35), "UML Class Diagram - Session, UI, Progression, and Scores", font=font(31, True), fill="#17365D")
    page = uml_box(d, 55, 190, 390, "MainPage", ["- gameSession: GameSession", "- highScoreManager: HighScoreManager"], ["+ MainPage()", "- menu and session event handlers"], "<<sealed Page>>")
    session = uml_box(d, 600, 150, 450, "GameSession", ["- enemies, projectiles, asteroids: List", "- state: GameState", "- currentLevel: int"], ["+ Start(), ReturnToMenu(): void", "+ HandleKeyDown/Up(key): void", "+ SetBoardSize(width, height): void", "+ ElementAdded/Removed events", "+ PlayerCreated, LevelChanged, Ended", "+ CampaignCompleted event"], "<<sealed IDisposable>>")
    scores = uml_box(d, 1160, 190, 390, "HighScoreManager", ["- campaignEntries: List<Entry>", "+ CampaignEntries, LastErrorMessage"], ["+ LoadCampaignScoresAsync(): Task", "+ Qualifies...(): bool", "+ AddCampaignEntryAsync(...): Task<int>"])
    logic = uml_box(d, 55, 720, 360, "GameLogic", [], ["+ CreatePiece(...): GamePiece", "+ IsCollision(piece1, piece2): bool"], "<<static>>")
    state = uml_box(d, 55, 1010, 300, "GameState", ["Menu", "Playing", "GameOver", "Won"], [], "<<enumeration>>")
    movement = uml_box(d, 480, 730, 330, "EnemyMovementState", ["+ Direction", "+ DirectionLockedUntil", "+ NextDirectionDecision"], ["+ EnemyMovementState(now, milliseconds)"], "<<sealed>>")
    combat = uml_box(d, 850, 730, 300, "EnemyCombatState", ["+ NextShotTime"], ["+ EnemyCombatState(seconds)"], "<<sealed>>")
    completion = uml_box(d, 1180, 730, 370, "CampaignCompletion", ["+ TimeTaken, Score", "+ CompletedLevel, Date"], ["+ CampaignCompletion(...)"], "<<sealed>>")
    entry = uml_box(d, 1080, 1050, 470, "HighScoreEntry", ["- name, timeTaken, score", "+ CompletedLevel, Date"], ["+ GetDelimitedLine(): string", "+ TryParse(...): bool", "+ IsValidName(name): bool"], "<<sealed>>")
    d.line((445, 380, 600, 380), fill="#17365D", width=4)
    d.polygon([(590, 380), (570, 368), (550, 380), (570, 392)], fill="#17365D")
    label(d, (480, 350), "1", 14); label(d, (575, 350), "1", 14)
    d.line((1050, 380, 1160, 380), fill="#17365D", width=4)
    d.polygon([(1042, 380), (1022, 368), (1002, 380), (1022, 392)], fill="#17365D")
    label(d, (1080, 350), "1", 14); label(d, (1135, 350), "1", 14)
    d.line((1355, scores[3], 1355, entry[1]), fill="#17365D", width=4)
    d.polygon([(1355, scores[3] + 8), (1343, scores[3] + 28), (1355, scores[3] + 48), (1367, scores[3] + 28)], fill="#17365D")
    label(d, (1380, 650), "0..10", 14)
    draw_dependency(d, (600, 535), (415, 770)); label(d, (485, 650), "uses", 14)
    d.line((715, session[3], 650, movement[1]), fill="#17365D", width=3)
    d.polygon([(715, session[3] + 5), (703, session[3] + 22), (715, session[3] + 39), (727, session[3] + 22)], fill="#17365D")
    label(d, (680, 675), "0..* per Enemy", 13)
    d.line((940, session[3], 1000, combat[1]), fill="#17365D", width=3)
    d.polygon([(940, session[3] + 5), (928, session[3] + 22), (940, session[3] + 39), (952, session[3] + 22)], fill="#17365D")
    label(d, (920, 675), "0..* per Enemy", 13)
    draw_dependency(d, (1010, session[3]), (1250, completion[1])); label(d, (1130, 660), "creates on win", 13)
    label(d, (55, 1260), "Filled diamond = composition/ownership. Dashed arrow = dependency. Multiplicities identify collection cardinality.", 15)
    img.save(path, quality=95)


def set_cell_shading(cell, fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:fill"), fill)
    tc_pr.append(shd)


def set_cell_margins(cell, top=90, start=110, bottom=90, end=110):
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for m, v in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{m}"))
        if node is None:
            node = OxmlElement(f"w:{m}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(v))
        node.set(qn("w:type"), "dxa")


def set_table_borders(table):
    tbl_pr = table._tbl.tblPr
    borders = tbl_pr.first_child_found_in("w:tblBorders")
    if borders is None:
        borders = OxmlElement("w:tblBorders")
        tbl_pr.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        tag = qn(f"w:{edge}")
        element = borders.find(tag)
        if element is None:
            element = OxmlElement(f"w:{edge}")
            borders.append(element)
        element.set(qn("w:val"), "single")
        element.set(qn("w:sz"), "6")
        element.set(qn("w:space"), "0")
        element.set(qn("w:color"), "D9D9D9")


def add_table(doc, headers, rows, widths=None):
    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    set_table_borders(table)
    for j, header in enumerate(headers):
        cell = table.rows[0].cells[j]
        cell.width = Inches(widths[j]) if widths else cell.width
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        set_cell_shading(cell, NAVY)
        set_cell_margins(cell)
        p = cell.paragraphs[0]
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run(header)
        r.bold = True
        r.font.color.rgb = RGBColor(255, 255, 255)
        r.font.size = Pt(9)
    for i, row in enumerate(rows):
        cells = table.add_row().cells
        for j, value in enumerate(row):
            cell = cells[j]
            cell.width = Inches(widths[j]) if widths else cell.width
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            set_cell_margins(cell)
            if i % 2:
                set_cell_shading(cell, PALE_GREY)
            p = cell.paragraphs[0]
            p.paragraph_format.space_after = Pt(0)
            p.paragraph_format.line_spacing = 1.05
            r = p.add_run(str(value))
            r.font.size = Pt(8.5)
    doc.add_paragraph().paragraph_format.space_after = Pt(2)
    return table


def add_field(paragraph, instruction, placeholder="Update field in Word"):
    run = paragraph.add_run()
    begin = OxmlElement("w:fldChar")
    begin.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = instruction
    separate = OxmlElement("w:fldChar")
    separate.set(qn("w:fldCharType"), "separate")
    text = OxmlElement("w:t")
    text.text = placeholder
    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    run._r.append(begin); run._r.append(instr); run._r.append(separate); run._r.append(text); run._r.append(end)


def add_page_number(paragraph):
    paragraph.add_run("Page ")
    add_field(paragraph, "PAGE", "1")


def configure_styles(doc):
    styles = doc.styles
    normal = styles["Normal"]
    normal.font.name = "Arial"
    normal._element.rPr.rFonts.set(qn("w:ascii"), "Arial")
    normal._element.rPr.rFonts.set(qn("w:hAnsi"), "Arial")
    normal.font.size = Pt(10.5)
    normal.font.color.rgb = BLACK
    normal.paragraph_format.space_after = Pt(7)
    normal.paragraph_format.line_spacing = 1.12
    title = styles["Title"]
    title.font.name = "Arial"
    title._element.rPr.rFonts.set(qn("w:ascii"), "Arial")
    title._element.rPr.rFonts.set(qn("w:hAnsi"), "Arial")
    title.font.size = Pt(31)
    title.font.bold = True
    title.font.color.rgb = BLACK
    for name, size in (("Heading 1", 17), ("Heading 2", 13), ("Heading 3", 11)):
        style = styles[name]
        style.font.name = "Arial"
        style._element.rPr.rFonts.set(qn("w:ascii"), "Arial")
        style._element.rPr.rFonts.set(qn("w:hAnsi"), "Arial")
        style.font.size = Pt(size)
        style.font.bold = True
        style.font.color.rgb = BLACK
        style.paragraph_format.space_before = Pt(16 if name == "Heading 1" else 10)
        style.paragraph_format.space_after = Pt(6)


def add_heading(doc, text, level=1):
    p = doc.add_paragraph(text, style=f"Heading {level}")
    p.paragraph_format.keep_with_next = True
    return p


def add_bullet(doc, text, level=0):
    p = doc.add_paragraph(style="List Bullet" if level == 0 else "List Bullet 2")
    p.add_run(text)
    return p


def add_class(doc, name, status, responsibility, members, constructors, methods, relationships):
    add_heading(doc, name, 2)
    p = doc.add_paragraph()
    p.add_run("Lab 1B status: ").bold = True
    p.add_run(status)
    p.add_run("  ")
    p.add_run("Responsibility: ").bold = True
    p.add_run(responsibility)
    add_bullet(doc, "Important fields and properties: " + members)
    if constructors:
        add_bullet(doc, "Constructors: " + constructors)
    if methods:
        add_bullet(doc, "Key methods and events: " + methods)
    add_bullet(doc, "Relationships: " + relationships)


def document_body(doc, domain_path, session_path):
    # Title page
    section = doc.sections[0]
    section.top_margin = Inches(0.8); section.bottom_margin = Inches(0.75)
    section.left_margin = Inches(0.82); section.right_margin = Inches(0.82)
    section.different_first_page_header_footer = True
    for _ in range(7):
        doc.add_paragraph()
    p = doc.add_paragraph(style="Title")
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.add_run("Lab 1B Documentation")
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run("DeepSpaceGalactic")
    r.bold = True; r.font.size = Pt(20); r.font.color.rgb = BLACK
    doc.add_paragraph()
    details = [("Student", "Student Name (to be confirmed)"), ("Course", "PROG 1621"), ("Lab", "Lab 1B"), ("Submission date", "October 24, 2026")]
    table = add_table(doc, ["", ""], details, [1.55, 4.5])
    for cell in table.rows[0].cells:
        cell.text = ""
        set_cell_shading(cell, "FFFFFF")
    for row in table.rows[1:]:
        row.cells[0].paragraphs[0].runs[0].bold = True
    doc.add_paragraph()
    p = doc.add_paragraph("Documentation for the current implementation of the DeepSpaceGalactic UWP game.")
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.runs[0].italic = True
    doc.add_page_break()

    add_heading(doc, "Table of Contents", 1)
    p = doc.add_paragraph()
    add_field(p, 'TOC \\o "1-2" \\h \\z \\u', "Open in Word and update fields to generate the table of contents.")
    p.runs[0].italic = True
    doc.add_paragraph("This document uses Word heading styles so the table of contents can be refreshed with Ctrl+A, then F9.")
    doc.add_page_break()

    add_heading(doc, "Project Description and Scope Specification", 1)
    doc.add_paragraph("DeepSpaceGalactic is a single-player, keyboard-controlled 2D space shooter implemented as a Universal Windows Platform application. The player pilots a ship within the lower half of the play area, fires upward at moving enemy ships, avoids enemy fire and asteroids, and progresses through a six-level campaign.")
    doc.add_paragraph("The project is intended for a player who wants a short arcade-style campaign with direct keyboard input and immediate visual feedback. The application presents a start menu, a play area with a heads-up display, an end-of-game overlay, and a top-ten campaign-time leaderboard.")
    add_heading(doc, "Implemented Gameplay Features", 2)
    add_table(doc, ["Feature", "Verified behaviour"], [
        ("Player movement", "Arrow keys move the player. Movement continues while a direction is held, supports diagonal movement, and is clamped to the lower half of the board and its edges."),
        ("Player firing", "Space fires an upward blue projectile when the player's fire-rate delay allows it. The configured starting fire rate is 0.5 seconds."),
        ("Enemy types", "Small, medium, and large enemies use different starting health, score value, speed, size, damage, and firing rate. Enemies move horizontally, may change direction, reverse at edges, and fire red projectiles downward."),
        ("Collisions and score", "Player projectiles can damage enemies and destroy asteroids. Destroyed enemies award their point value; destroyed asteroids award 10 points only when hit by a player projectile. Enemy projectiles and asteroid contact damage the player."),
        ("Campaign progression", "The campaign begins at level 1 and is won after completing level 6. Enemy counts increase every two levels, and enemy health, damage, and firing rate are scaled by level."),
        ("Asteroid systems", "Asteroids spawn from the top, left, or right at random sizes and velocities. After level 3 is cleared, a 35-second asteroid belt begins before level 4."),
        ("Leaderboard", "On a campaign win, a qualifying completion time can be saved with a validated pilot name. The local top-ten list is ordered by fastest time, then higher score, then earlier date."),
    ], [1.65, 4.95])
    add_heading(doc, "Scope Boundaries and Known Limitations", 2)
    add_bullet(doc, "The source exposes only arrow-key movement and the Space key for play. No pause, settings, difficulty-selection, or audio controls are implemented in the current UI.")
    add_bullet(doc, "The player is limited to the lower half of the board. Gameplay is single-player; no networking, multiplayer, or online leaderboard implementation is present.")
    add_bullet(doc, "The leaderboard is saved in the app's local storage as campaign_highscores.txt. It is not shared across users or devices. Load and save failures are caught and displayed as a message.")
    add_bullet(doc, "The application includes TODO comments for saving and restoring suspended application state. A suspended session is therefore not documented as resumable.")
    add_bullet(doc, "The current UI shows numeric score, health, lives, and level values, but has no explicit textual labels for the first three HUD numbers.")

    doc.add_page_break()
    add_heading(doc, "Class Descriptions and UML Diagrams", 1)
    doc.add_paragraph("The following inventory covers the classes and enumeration that implement or directly support the current game. Git has no commit, tag, branch, or repository document explicitly identified as the Lab 1A baseline. The statuses below use the available master-to-current comparison as evidence, but do not represent a verified Lab 1A boundary.")
    add_heading(doc, "Lab 1B Inventory Evidence", 2)
    add_table(doc, ["Evidence category", "Types"], [
        ("Added after the repository master reference", "GameSession; CampaignCompletion; HighScoreEntry; HighScoreManager; EnemyCombatState; GameState."),
        ("Modified after the repository master reference", "MainPage; Asteroid; Enemy; GameLogic; LargeEnemy; Player; Projectile; SmallEnemy."),
        ("Current support types with no change after that reference", "App; GamePiece; MediumEnemy; EnemyMovementState. These types are relevant to the current implementation but cannot be confirmed as Lab 1B work."),
    ], [2.1, 4.5])
    add_heading(doc, "UML Notation", 2)
    doc.add_paragraph("The diagrams use standard UML class compartments. A plus sign (+) marks public visibility and a minus sign (-) marks private visibility. Solid lines ending in hollow triangles represent inheritance. Filled diamonds represent ownership/composition, while directed lines indicate use or dependency. Multiplicity labels identify the number of related objects where the code maintains a collection.")
    landscape = doc.add_section(WD_SECTION.NEW_PAGE)
    landscape.orientation = WD_ORIENT.LANDSCAPE
    landscape.page_width = Inches(11)
    landscape.page_height = Inches(8.5)
    landscape.top_margin = Inches(0.55); landscape.bottom_margin = Inches(0.5)
    landscape.left_margin = Inches(0.6); landscape.right_margin = Inches(0.6)
    add_heading(doc, "Game Object Model", 2)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.add_run().add_picture(str(domain_path), width=Inches(9.0))
    p = doc.add_paragraph("Figure 1. Game object inheritance and key public members.")
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.runs[0].italic = True
    doc.add_page_break()
    add_heading(doc, "Session and Application Model", 2)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.add_run().add_picture(str(session_path), width=Inches(8.0))
    p = doc.add_paragraph("Figure 2. UI, session, state, completion, and leaderboard relationships.")
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.runs[0].italic = True

    portrait = doc.add_section(WD_SECTION.NEW_PAGE)
    portrait.orientation = WD_ORIENT.PORTRAIT
    portrait.page_width = Inches(8.5)
    portrait.page_height = Inches(11)
    portrait.top_margin = Inches(0.8); portrait.bottom_margin = Inches(0.75)
    portrait.left_margin = Inches(0.82); portrait.right_margin = Inches(0.82)
    add_heading(doc, "Class Inventory and Responsibilities", 2)
    add_class(doc, "MainPage", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Owns the XAML page and presents the menu, gameplay view, HUD, end-game overlay, and leaderboard.", "Readonly GameSession and HighScoreManager references; loadCampaignScoresTask; named XAML controls including the HUD, menu, overlays, and leaderboard text blocks.", "MainPage().", "Event handlers for page load/unload, board resizing, keys, game-session presentation events, Start Game, Play Again, and Main Menu. It loads/saves leaderboard interaction through HighScoreManager.", "Subscribes to GameSession events; binds the HUD to Player; composes a HighScoreManager; inherits from Page.")
    add_class(doc, "GameSession", "Added after the repository master reference; Lab 1B membership requires confirmation.", "Owns game rules and active-session state independently of the visual tree.", "Collections of enemies, projectiles, asteroids, held keys, and per-enemy movement/combat state; timers; Player; level and campaign timing data; GameState.", "GameSession().", "Public Start, ReturnToMenu, SetBoardSize, HandleKeyDown, HandleKeyUp, and Dispose. Events: ElementAdded, ElementRemoved, BoardCleared, PlayerCreated, LevelChanged, Ended, and CampaignCompleted.", "Creates Player, Enemy subclasses, Projectile, and Asteroid instances; uses GameLogic and GameState; emits CampaignCompletion; is composed by MainPage.")
    add_class(doc, "CampaignCompletion", "Added after the repository master reference; Lab 1B membership requires confirmation.", "Carries the final campaign result from a winning GameSession to the page.", "Read-only TimeTaken, Score, CompletedLevel, and Date.", "CampaignCompletion(timeTaken, score, completedLevel, date).", "No additional methods or events.", "Created by GameSession on a win and consumed by MainPage to decide whether to request a leaderboard name.")
    add_class(doc, "HighScoreEntry", "Added after the repository master reference; Lab 1B membership requires confirmation.", "Represents one validated, serializable campaign leaderboard result.", "Name, TimeTaken, Score, CompletedLevel, and Date with validation in their setters.", "HighScoreEntry(name, timeTaken, score, completedLevel, date).", "GetDelimitedLine serializes to colon-delimited text. Static TryParse reads a saved line; static IsValidName enforces one to 24 characters with no colons or line breaks.", "Stored by HighScoreManager and shown by MainPage.")
    add_class(doc, "HighScoreManager", "Added after the repository master reference; Lab 1B membership requires confirmation.", "Loads, validates, ranks, limits, and saves the campaign leaderboard in local app storage.", "campaignEntries list; CampaignEntries read-only list view; LastErrorMessage; a maximum of 10 entries.", "Implicit parameterless constructor.", "LoadCampaignScoresAsync, QualifiesForCampaignLeaderboard, and AddCampaignEntryAsync. Private methods save, sort, limit, and compare entries.", "Composes 0..10 HighScoreEntry objects; used by MainPage.")
    add_class(doc, "GameState", "Added after the repository master reference; Lab 1B membership requires confirmation.", "Enumeration that records the session lifecycle.", "Values: Menu, Playing, GameOver, Won.", "Not applicable.", "Not applicable.", "Stored by GameSession to gate input, simulation, cleanup, and end-game processing.")

    doc.add_page_break()
    add_class(doc, "GamePiece", "Current support type; no change after the repository master reference, so Lab 1B membership cannot be confirmed.", "Base visual game object that wraps one XAML Image and its margin-based position.", "Private objectMargins and onScreen; public Img and Position read-only properties.", "GamePiece(Image img).", "Move(horizontalDistance, verticalDistance) and Move(VirtualKey direction), each returning whether a movement occurred.", "Superclass of Player, Enemy, Asteroid, and Projectile; used by GameLogic collision and creation helpers.")
    add_class(doc, "Player", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Represents the player ship, its mutable combat statistics, and HUD-bound values.", "Lives, Health, FireRate, Speed, Score, and Damage properties; PropertyChanged event. Starting values are 3 lives, 100 health, 0.5 fire rate, 9 speed, 0 score, and 1 damage.", "Player(Image img); Player(Image img, lives, health, firerate, speed, score, damage).", "Raises PropertyChanged when its public state changes.", "Inherits GamePiece and implements INotifyPropertyChanged; owned by GameSession and bound to MainPage's HUD.")
    add_class(doc, "Enemy", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Base enemy ship with common statistics used by AI movement, firing, damage, and scoring.", "Health, PointValue, Speed, Damage, FireRate, and Size properties with validation/defaulting.", "Enemy(Image img).", "No additional public methods or events.", "Inherits GamePiece; superclass of SmallEnemy, MediumEnemy, and LargeEnemy; GameSession maintains active enemies and their movement/combat state.")
    add_class(doc, "SmallEnemy", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Configures the small enemy variant.", "Starting values: Health 1, PointValue 150, Speed 5.3, Size 35, Damage 20, FireRate 1.0.", "SmallEnemy(Image img).", "No additional methods or events.", "Inherits Enemy; instantiated by GameSession's generic enemy creation method.")
    add_class(doc, "MediumEnemy", "Current support type; no change after the repository master reference, so Lab 1B membership cannot be confirmed.", "Configures the medium enemy variant.", "Starting values: Health 2, PointValue 100, Speed 3.6, Size 40, Damage 35, FireRate 1.5.", "MediumEnemy(Image img).", "No additional methods or events.", "Inherits Enemy; instantiated by GameSession's generic enemy creation method.")
    add_class(doc, "LargeEnemy", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Configures the large enemy variant.", "Starting values: Health 3, PointValue 300, Speed 2.3, Size 60, Damage 50, FireRate 3.0.", "LargeEnemy(Image img).", "No additional methods or events.", "Inherits Enemy; instantiated by GameSession's generic enemy creation method.")

    doc.add_page_break()
    add_class(doc, "Asteroid", "Modified after the repository master reference; Lab 1B membership requires confirmation. The source file is named Astriod.cs, while the class name is Asteroid.", "Represents a destructible hazard that moves with independent horizontal and vertical velocities.", "VelocityX, VelocityY, Health, Damage, and PointValue. Constructor defaults: health 1, damage 50, point value 10.", "Asteroid(Image img).", "No additional methods or events.", "Inherits GamePiece; held by GameSession and tested against Player and Projectile through GameLogic.IsCollision.")
    add_class(doc, "Projectile", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Represents either a player or enemy laser and its movement/damage data.", "VelocityX, VelocityY, Damage, and IsPlayerProjectile. The default constructor creates a player projectile with damage 1.", "Projectile(Image img); Projectile(Image img, velocityX, velocityY, damage, isPlayerProjectile).", "No additional methods or events.", "Inherits GamePiece; GameSession creates and removes projectiles and uses their ownership flag to route collisions.")
    add_class(doc, "GameLogic", "Modified after the repository master reference; Lab 1B membership requires confirmation.", "Static utility class for visual piece creation and rectangle collision detection.", "No instance fields. Private GetImageDimension selects a specified image dimension or ActualWidth/ActualHeight.", "Not applicable; static class.", "CreatePiece(imageSource, left, top); overloaded CreatePiece(imageSource, size, left, top); IsCollision(piece1, piece2).", "Creates GamePiece objects from packaged assets and is used by GameSession for creation and collision tests.")
    add_class(doc, "EnemyMovementState", "Current support type; no change after the repository master reference, so Lab 1B membership cannot be confirmed.", "Stores the horizontal-direction decision timing for a single enemy.", "Direction; DirectionLockedUntil; NextDirectionDecision.", "EnemyMovementState(now, directionDecisionMilliseconds).", "No additional methods or events.", "Composed in GameSession's dictionary with one state object per active Enemy.")
    add_class(doc, "EnemyCombatState", "Added after the repository master reference; Lab 1B membership requires confirmation.", "Stores the next permitted shot time for one enemy.", "NextShotTime.", "EnemyCombatState(seconds).", "No additional methods or events.", "Composed in GameSession's dictionary with one state object per active Enemy.")
    add_class(doc, "App", "Current application bootstrap; no change after the repository master reference, so Lab 1B membership cannot be confirmed.", "Initializes the UWP application and launches MainPage.", "No game-specific fields in the inspected source.", "App().", "OnLaunched and OnSuspending override handlers.", "Starts MainPage. The suspension handler includes a TODO rather than saved gameplay state.")

    doc.add_page_break()
    add_heading(doc, "User Manual and Guide", 1)
    add_heading(doc, "Launching the Game", 2)
    doc.add_paragraph("Open the DeepSpaceGalactic UWP solution in Visual Studio, select a supported target platform, build the solution, and start debugging or deploy the application. The package manifest identifies DeepSpaceGalactic as the application entry, and the App class opens MainPage. The opening screen is the main menu.")
    add_heading(doc, "Menus and Selections", 2)
    add_table(doc, ["Screen or control", "Actual behaviour"], [
        ("Main menu", "Shows the game image, a START GAME button, a campaign top-ten list, and any leaderboard load error."),
        ("START GAME", "Loads campaign scores if needed, hides the menu, clears prior session objects, creates a new player, starts level 1, and starts the simulation."),
        ("End-game overlay", "Appears with GAME OVER or YOU WIN! after a session ends. It displays campaign leaderboard information and any completion feedback."),
        ("PLAY AGAIN", "Starts a new session after ensuring campaign scores are loaded."),
        ("MAIN MENU", "Returns the GameSession to the menu state, clears active game objects, hides the overlay, and shows the menu."),
        ("Leaderboard name prompt", "On a winning time that qualifies for the top ten, prompts for a pilot name up to 24 characters. A name cannot contain a colon or line break."),
    ], [1.6, 5.0])
    add_heading(doc, "Controls", 2)
    add_table(doc, ["Input", "Action"], [
        ("Up arrow", "Move upward within the lower half of the play area."),
        ("Down arrow", "Move downward within the lower half of the play area."),
        ("Left arrow", "Move left without leaving the play-area edge."),
        ("Right arrow", "Move right without leaving the play-area edge."),
        ("Two direction arrows", "Move diagonally at a normalized speed."),
        ("Space", "Fire an upward blue laser, subject to the player's fire-rate cooldown."),
    ], [1.6, 5.0])
    add_heading(doc, "HUD and Output Displays", 2)
    add_table(doc, ["Location", "Displayed value and meaning"], [
        ("Top left", "Score: numeric points earned by destroying enemies and player-shot asteroids."),
        ("Top centre-left", "Health: current player health. It begins at 100 and is restored to the starting value after a non-final life loss."),
        ("Top centre-right", "Lives: remaining lives. A level begins with 3 lives."),
        ("Top right", "Level: current campaign level, beginning with Level: 1."),
        ("Menu and end overlay", "Campaign Top 10: rank, pilot, time in seconds, score, completed level, and date for stored campaign completions."),
    ], [1.6, 5.0])
    add_heading(doc, "Objective, Progression, and End Conditions", 2)
    doc.add_paragraph("Destroy every enemy in each numbered level while avoiding enemy lasers and asteroids. The game starts at level 1. Levels 1 and 2 use one enemy of each size; levels 3 and 4 use two of each; levels 5 and 6 use three of each. After level 3 is cleared, survive the 35-second asteroid belt. After clearing level 6, the game displays YOU WIN! and creates a campaign completion result.")
    doc.add_paragraph("Damage reduces health. When a hit reduces a player with positive health to zero, one life is lost and health is restored unless that was the last life. The game displays GAME OVER when the final life is lost. Projectiles and asteroids that leave the board are removed.")

    doc.add_page_break()
    add_heading(doc, "Change Log Since Lab 1A", 1)
    doc.add_paragraph("No repository marker identifies which commit was submitted for Lab 1A. The items below are verified from commit history after the repository master reference, but their classification as changes since Lab 1A requires confirmation against the submitted Lab 1A version.")
    add_table(doc, ["Category", "Verified history evidence", "Lab 1A comparison status"], [
        ("Combat and collision", "Player shot cooldown; enemy projectiles; projectile damage and ownership; collision/removal logic; asteroid spawning; enemy and asteroid damage and score handling were introduced or updated in commits from October 2 through October 6.", "Requires Lab 1A baseline confirmation."),
        ("Progression", "GameState, menu/end-game flow, complete levels, level-based win checks, and changing enemy statistics/positioning appear in October 6-7 commits. Current code implements six levels and an asteroid belt after level 3.", "Requires Lab 1A baseline confirmation."),
        ("Architecture", "The October 20 commit records decoupling MainPage from GameSession. Current code has GameSession own rules and emit events for MainPage to render.", "Requires Lab 1A baseline confirmation."),
        ("Leaderboard and UI", "The October 24 history records campaign leaderboard file I/O and a major UI update. Current code contains HighScoreEntry, HighScoreManager, menu/overlay leaderboard displays, and local persistence.", "Requires Lab 1A baseline confirmation."),
        ("Asteroid belt and tuning", "The October 24 history records the asteroid belt between levels 3 and 4 and reduced asteroid damage. Current code uses a 35-second belt and asteroid damage of 50.", "Requires Lab 1A baseline confirmation."),
        ("Assets", "History records background and game-image asset additions or updates. Current XAML uses a space background and a DeepRockGalactic image on the menu.", "Requires Lab 1A baseline confirmation."),
    ], [1.25, 3.85, 1.5])
    add_heading(doc, "Confirmation Needed Before Submission", 2)
    doc.add_paragraph("Provide the actual Lab 1A submission commit, archive, or instructor-confirmed baseline if the change log must state exactly what changed since Lab 1A. The current repository evidence is sufficient to describe implementation history, but not to certify the lab boundary.")

    doc.add_page_break()
    add_heading(doc, "Bug Report", 1)
    doc.add_paragraph("No separate issue tracker or bug-note file is present in the repository. The following entries are supported by commit history and current source behavior; the final row remains open because the current source explicitly contains TODOs.")
    add_table(doc, ["Issue", "Effect", "Resolution or current evidence", "Status"], [
        ("Asteroid points when an enemy destroys an asteroid", "The commit history identifies incorrect asteroid point handling when an enemy projectile destroyed an asteroid.", "Current collision code adds asteroid points only when Projectile.IsPlayerProjectile is true, so enemy projectiles do not award player points.", "Resolved in code; history cites October 6."),
        ("Enemy initial firing timing", "Enemies could begin firing together at the start of a game, creating an immediate synchronized volley.", "EnemyCombatState initializes NextShotTime using a random one-to-two-second delay; later shots use each enemy's FireRate.", "Resolved in code; history cites October 6."),
        ("Health/lives startup behaviour", "History records a fix for health and lives adding more health at startup.", "Current Player defaults to 3 lives and 100 health; GameSession captures starting health and restores it after a non-final life loss.", "Reported resolved by October 6 history; exact original reproduction requires confirmation."),
        ("Suspension-state persistence", "A suspended game is not shown as resumable from its current position.", "App.OnLaunched and App.OnSuspending retain TODO comments for load/save application state. No implementation was found in the inspected source.", "Open limitation; confirmation needed if it was intentionally out of scope."),
    ], [1.45, 1.55, 2.75, 1.0])
    add_heading(doc, "Repository Evidence Reviewed", 2)
    doc.add_paragraph("The documentation was based on the current C# source, MainPage XAML, app manifest, project files, and available Git history through the current HEAD commit dated October 24, 2026. No game source, XAML, or project configuration was changed while producing this document.")


def configure_header_footer(doc):
    for section in doc.sections:
        header = section.header
        header.is_linked_to_previous = False
        p = header.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
        p.paragraph_format.space_after = Pt(0)
        r = p.add_run("DeepSpaceGalactic  |  PROG 1621  |  Lab 1B")
        r.font.name = "Arial"; r.font.size = Pt(8); r.font.color.rgb = RGBColor(70, 70, 70)
        footer = section.footer
        footer.is_linked_to_previous = False
        p = footer.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_before = Pt(0)
        r = p.add_run("DeepSpaceGalactic Documentation  |  ")
        r.font.name = "Arial"; r.font.size = Pt(8); r.font.color.rgb = RGBColor(70, 70, 70)
        add_page_number(p)


def main():
    DIAGRAMS.mkdir(parents=True, exist_ok=True)
    domain_path = DIAGRAMS / "game_object_model_uml.png"
    session_path = DIAGRAMS / "session_application_model_uml.png"
    build_domain_diagram(domain_path)
    build_session_diagram(session_path)
    doc = Document()
    configure_styles(doc)
    document_body(doc, domain_path, session_path)
    configure_header_footer(doc)
    doc.core_properties.title = "Lab 1B Documentation DeepSpaceGalactic"
    doc.core_properties.subject = "PROG 1621 Lab 1B Documentation"
    doc.core_properties.author = "Student Name to be confirmed"
    settings = doc.settings.element
    update = OxmlElement("w:updateFields")
    update.set(qn("w:val"), "true")
    settings.append(update)
    doc.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
