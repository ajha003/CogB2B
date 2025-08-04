# IdentityApp - Extended User Profile

This ASP.NET Core application demonstrates how to extend the default ASP.NET Core Identity user model with additional properties.

## Features Added

### Extended User Properties
The `ApplicationUser` class extends `IdentityUser` with the following additional properties:

- **About Me**: A text field for users to describe themselves (max 1000 characters)
- **Service Category**: The type of service the user provides (max 100 characters)
- **Location**: User's location (max 200 characters)
- **Banner Image**: URL for a banner image (max 500 characters)
- **Profile Picture**: URL for a profile picture (max 500 characters)
- **Alternative Phone**: An additional phone number (max 20 characters)

### Updated Pages

1. **Registration Page** (`/Identity/Account/Register`)
   - Extended form with all new properties
   - All fields are optional during registration

2. **Profile Management** (`/Identity/Account/Manage`)
   - Users can edit all their profile information
   - Form includes all the new properties

3. **Profile Display** (`/Profile`)
   - Public profile page showing user information
   - Displays banner image, profile picture, and all user details
   - Only accessible to authenticated users

## Database Migration

The application includes a migration that adds the new columns to the `AspNetUsers` table:

```sql
ALTER TABLE "AspNetUsers" ADD "AboutMe" TEXT NULL;
ALTER TABLE "AspNetUsers" ADD "AlternativePhone" TEXT NULL;
ALTER TABLE "AspNetUsers" ADD "BannerImage" TEXT NULL;
ALTER TABLE "AspNetUsers" ADD "Location" TEXT NULL;
ALTER TABLE "AspNetUsers" ADD "ProfilePic" TEXT NULL;
ALTER TABLE "AspNetUsers" ADD "ServiceCategory" TEXT NULL;
```

## How to Use

1. **Register a new account** at `/Identity/Account/Register`
2. **Edit your profile** at `/Identity/Account/Manage`
3. **View your profile** at `/Profile`

## Technical Implementation

### Key Files Modified/Created:

- `Data/ApplicationUser.cs` - Extended user model
- `Data/ApplicationDbContext.cs` - Updated to use ApplicationUser
- `Program.cs` - Updated Identity configuration
- `Areas/Identity/Pages/Account/Register.cshtml` - Extended registration form
- `Areas/Identity/Pages/Account/Register.cshtml.cs` - Updated registration logic
- `Areas/Identity/Pages/Account/Manage/Index.cshtml` - Extended profile management form
- `Areas/Identity/Pages/Account/Manage/Index.cshtml.cs` - Updated profile management logic
- `Pages/Profile.cshtml` - New profile display page
- `Pages/Profile.cshtml.cs` - Profile display logic
- `Pages/Shared/_LoginPartial.cshtml` - Updated navigation

## Running the Application

```bash
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Database

The application uses SQLite by default. The database file is created automatically when you first run the application.